using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Api.Pubg;
using Villupp.PubgStatsBot.Config;
using Villupp.PubgStatsBot.Services;
using Villupp.PubgStatsBot.TableStorage;
using Villupp.PubgStatsBot.TableStorage.Models;
using Villupp.PubgStatsBot.TableStorage.Repositories;

namespace Villupp.PubgStatsBot.PubgLeaderboards
{
    public class PubgLeaderboardPoller(
        ILogger<PubgLeaderboardPoller> logger,
        PubgApiClient pubgClient,
        PubgStatsBotSettings botSettings,
        PubgSeasonRepository seasonRepository,
        TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService,
        PubgPlayerService playerService
        )
    {
        private const int STORAGE_REQUEST_BATCH_SIZE = 50;

        private readonly ILogger<PubgLeaderboardPoller> logger = logger;
        private readonly PubgApiClient pubgClient = pubgClient;
        private readonly PubgStatsBotSettings botSettings = botSettings;
        private readonly PubgSeasonRepository seasonRepository = seasonRepository;
        private readonly TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService = lbPlayerTableService;
        private readonly PubgPlayerService playerService = playerService;

        private bool isPollerRunning = false;

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
                    foreach (var region in PubgApiClient.Regions)
                    {
                        var currentSeason = await seasonRepository.GetCurrentSeason();
                        logger.LogDebug($"Polling for PUBG leaderboard {region} {currentSeason.Id}");
                        await UpdateLeaderboards(region, currentSeason.Id);
                        var delay = 10000;
                        await Task.Delay(delay);
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

            var lbPlayersToDelete = await lbPlayerTableService.Get(p => p.Region == region && p.Season == seasonId);
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

                    addTasks = [];
                }
            }

            Task.WaitAll([.. addTasks]);

            logger.LogInformation($"Added {seasonLbPlayers.Count} current season leaderboard players");

            foreach (var player in seasonLbPlayers)
            {
                var playerName = player.attributes.name;
                await playerService.GetOrCreatePubgPlayer(playerName);
            }

            logger.LogInformation($"UpdateLeaderboards season {seasonId} done.");
        }
    }
}