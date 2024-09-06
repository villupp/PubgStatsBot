using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Api.Pubg;
using Villupp.PubgStatsBot.Config;
using Villupp.PubgStatsBot.TableStorage;
using Villupp.PubgStatsBot.TableStorage.Models;
using Villupp.PubgStatsBot.TableStorage.Repositories;

namespace Villupp.PubgStatsBot.PubgLeaderboards
{
    public class PubgLeaderboardPoller
    {
        private const int STORAGE_REQUEST_BATCH_SIZE = 50;

        private readonly ILogger<PubgLeaderboardPoller> logger;
        private readonly PubgApiClient pubgClient;
        private readonly PubgStatsBotSettings botSettings;
        private PubgSeasonRepository seasonRepository;
        private TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService;
        private TableStorageService<PubgPlayer> playerTableService;

        private bool isPollerRunning;

        public PubgLeaderboardPoller(
            ILogger<PubgLeaderboardPoller> logger,
            PubgApiClient pubgClient,
            PubgStatsBotSettings botSettings,
            PubgSeasonRepository seasonRepository,
            TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService,
            TableStorageService<PubgPlayer> playerTableService
            )
        {
            this.isPollerRunning = false;
            this.logger = logger;
            this.pubgClient = pubgClient;
            this.botSettings = botSettings;
            this.seasonRepository = seasonRepository;
            this.lbPlayerTableService = lbPlayerTableService;
            this.playerTableService = playerTableService;
        }

        public async Task Start()
        {
            logger.LogInformation("PubgLeaderboardPoller.Start called");

            if (isPollerRunning)
            {
                logger.LogInformation("PUBG Leaderboard poller is already running!");
                return;
            }

            var timer = new PeriodicTimer(TimeSpan.FromMinutes(botSettings.PubgLeaderboardPollingIntervalMinutes));
            logger.LogInformation("PubgLeaderboardPoller polling service started");

            isPollerRunning = true;

            do
            {
                try
                {
                    logger.LogDebug("Polling for PUBG leaderboards");

                    var seasons = new string[] {
                        "division.bro.official.pc-2018-20",
                        "division.bro.official.pc-2018-21",
                        "division.bro.official.pc-2018-22",
                        "division.bro.official.pc-2018-23",
                        "division.bro.official.pc-2018-24",
                        "division.bro.official.pc-2018-25",
                        "division.bro.official.pc-2018-26",
                        "division.bro.official.pc-2018-27",
                        "division.bro.official.pc-2018-28",
                        "division.bro.official.pc-2018-29",
                        "division.bro.official.pc-2018-30",
                        "division.bro.official.pc-2018-31"
                    };

                    foreach (var seasonId in seasons)
                    {
                        foreach (var region in PubgApiClient.Regions)
                            await UpdateLeaderboards(region, seasonId);
                        // var currentSeason = await seasonRepository.GetCurrentSeason();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"PubgLeaderboardPoller iteration failed: {ex}");
                }
            } while (await timer.WaitForNextTickAsync());
        }

        private async Task UpdateLeaderboards(string region, string seasonId)
        {
            logger.LogInformation($"UpdateLeaderboards region {region} season {seasonId}");

            var seasonLbPlayers = await pubgClient.GetLeaderboardPlayers(region, seasonId);

            if (seasonLbPlayers == null || seasonLbPlayers.Count == 0)
                return;

            logger.LogInformation($"Got {seasonLbPlayers.Count} {region} season {seasonId} leaderboard players");

            var lbPlayersToDelete = await lbPlayerTableService.Get(p => p.Season == seasonId);
            var deleteTasks = new List<Task>();

            for (int i = 0; i < lbPlayersToDelete.Count; i++)
            {
                var lbPlayerToDelete = lbPlayersToDelete[i];
                deleteTasks.Add(lbPlayerTableService.Delete(lbPlayerToDelete));

                if (deleteTasks.Count == STORAGE_REQUEST_BATCH_SIZE
                    || i == lbPlayersToDelete.Count - 1)
                {
                    logger.LogInformation($"Deleting {deleteTasks.Count} region {region} season {seasonId} leaderboard players");
                    Task.WaitAll(deleteTasks.ToArray());

                    deleteTasks = [];
                }
            }

            var addTasks = new List<Task>();

            logger.LogInformation($"Deleted {lbPlayersToDelete.Count} region {region} season {seasonId} leaderboard players");
            logger.LogInformation($"Adding {seasonLbPlayers.Count} region {region} season {seasonId} leaderboard players");

            for (int i = 0; i < seasonLbPlayers.Count; i++)
            {
                var lbPlayer = seasonLbPlayers[i];

                var now = DateTime.UtcNow;
                var lbPlayerToAdd = new PubgLeaderboardPlayer()
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "",
                    Name = lbPlayer?.attributes?.name,
                    Id = lbPlayer.id,
                    Rank = lbPlayer.attributes.rank,
                    Season = seasonId,
                    Timestamp = now,
                    AvgDamage = lbPlayer?.attributes?.stats?.averageDamage,
                    KdaRatio = lbPlayer?.attributes?.stats?.kda,
                    //KdRatio = lbPlayer?.attributes?.stats?.killDeathRatio,
                    GameCount = lbPlayer?.attributes?.stats?.games,
                    WinCount = lbPlayer?.attributes?.stats?.wins,
                    Rp = lbPlayer?.attributes?.stats?.rankPoints,
                    //WinRatio = lbPlayer?.attributes?.stats?.winRatio
                    Tier = lbPlayer?.attributes?.stats?.tier,
                    SubTier = lbPlayer?.attributes?.stats?.subTier,
                    Region = region
                };

                lbPlayerToAdd.WinRatio = lbPlayerToAdd.WinCount == 0 ? 0 : (decimal)lbPlayerToAdd.WinCount / (decimal)lbPlayerToAdd.GameCount;

                addTasks.Add(lbPlayerTableService.Add(lbPlayerToAdd));

                if (addTasks.Count == STORAGE_REQUEST_BATCH_SIZE
                    || i == seasonLbPlayers.Count - 1)
                {
                    logger.LogInformation($"Adding {addTasks.Count} current season leaderboard players");
                    Task.WaitAll(addTasks.ToArray());

                    addTasks = new List<Task>();
                }
            }

            Task.WaitAll(addTasks.ToArray());

            logger.LogInformation($"Added {seasonLbPlayers.Count} current season leaderboard players");

            foreach (var player in seasonLbPlayers)
            {
                var playerName = player.attributes.name;

                var existingPubgPlayers = await playerTableService.Get(p => p.DisplayName == playerName);
                if (existingPubgPlayers.Count > 0)
                    continue;

                logger.LogInformation($"Adding {playerName} to storage");

                var pubgPlayer = new PubgPlayer()
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "",
                    Id = player.id,
                    Name = playerName.ToLower(),
                    DisplayName = playerName,
                };

                await playerTableService.Add(pubgPlayer);

                logger.LogInformation($"Added {playerName} to storage");
            }

            logger.LogInformation($"UpdateLeaderboards season {seasonId} done.");
        }
    }
}