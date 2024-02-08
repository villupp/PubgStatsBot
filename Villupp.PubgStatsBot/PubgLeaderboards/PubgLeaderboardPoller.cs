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

                    var currentSeason = await seasonRepository.GetCurrentSeason();
                    await UpdateLeaderboards(currentSeason.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError($"PubgLeaderboardPoller iteration failed: {ex}");
                }
            } while (await timer.WaitForNextTickAsync());
        }

        private async Task UpdateLeaderboards(string seasonId)
        {
            logger.LogInformation($"UpdateLeaderboards season {seasonId}");

            var seasonLbPlayers = await pubgClient.GetLeaderboardPlayers(seasonId);

            if (seasonLbPlayers == null || seasonLbPlayers.Count == 0)
                return;

            logger.LogInformation($"Got {seasonLbPlayers.Count} season {seasonId} leaderboard players");

            var lbPlayersToDelete = await lbPlayerTableService.Get(p => p.Season == seasonId);
            var deleteTasks = new List<Task>();

            foreach (var lbPlayerToDelete in lbPlayersToDelete)
                deleteTasks.Add(lbPlayerTableService.Delete(lbPlayerToDelete));

            logger.LogInformation($"Deleting {lbPlayersToDelete.Count} current season leaderboard players");
            Task.WaitAll(deleteTasks.ToArray());

            var addTasks = new List<Task>();

            logger.LogInformation($"Deleted {lbPlayersToDelete.Count} current season leaderboard players");
            logger.LogInformation($"Adding {seasonLbPlayers.Count} current season leaderboard players");

            foreach (var lbPlayer in seasonLbPlayers)
            {
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
                };

                lbPlayerToAdd.WinRatio = lbPlayerToAdd.WinCount / lbPlayerToAdd.GameCount;

                addTasks.Add(lbPlayerTableService.Add(lbPlayerToAdd));
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