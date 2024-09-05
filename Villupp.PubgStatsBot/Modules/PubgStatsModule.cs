using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Api;
using Villupp.PubgStatsBot.Api.Pubg.Models;
using Villupp.PubgStatsBot.CommandHandlers.PubgStats;
using Villupp.PubgStatsBot.TableStorage.Models;
using Villupp.PubgStatsBot.TableStorage.Repositories;

namespace Villupp.PubgStatsBot.Modules
{
    [Group("pubgstats", "")]
    public class PubgStatsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogger logger;
        private PubgStatsHandler pubgStatsHandler;
        private PubgSeasonRepository seasonRepository;

        public PubgStatsModule(ILogger<PubgStatsModule> logger, PubgStatsHandler pubgStatsHandler, PubgSeasonRepository seasonRepository)
        {
            this.logger = logger;
            this.pubgStatsHandler = pubgStatsHandler;
            this.seasonRepository = seasonRepository;
        }

        // Posts PUBG player stats for current ongoing season
        [SlashCommand("player", "")]
        public async Task SeasonRankedStats(string playername, bool ispublic = false, int season = -1)
        {
            logger.LogInformation($"SeasonRankedStats initiated by {Context.User.Username} for player '{playername}', season {season}");

            try
            {
                if (string.IsNullOrEmpty(playername))
                {
                    await RespondAsync("Provide a player name.", ephemeral: true);
                    return;
                }

                if (season != -1 && season < 7)
                {
                    await RespondAsync("Ranked season stats are available from season 7 and later.", ephemeral: true);
                    return;
                }

                await RespondAsync($"Retrieving stats for {playername}..", ephemeral: !ispublic);

                var player = await pubgStatsHandler.GetPlayer(playername);
                var currentSeason = await seasonRepository.GetCurrentSeason();
                PubgSeason statsSeason = null;

                if (season > currentSeason.SeasonNumber)
                    statsSeason = currentSeason;
                else
                    statsSeason = await seasonRepository.GetSeason(season);

                if (player == null)
                {
                    logger.LogInformation("Could not retrieve player. Stats not posted.");
                    await ModifyOriginalResponseAsync((msg) => msg.Content = "Player not found.");
                    return;
                }

                if (statsSeason == null)
                {
                    logger.LogInformation("Could not retrieve season. Stats not posted.");
                    await ModifyOriginalResponseAsync((msg) => msg.Content = "Ranked season not found. There might be an issue. Use `refreshseasons` command to refresh season cache.");
                    return;
                }

                PubgStatsMessage statsMsg = null;
                RankedStats seasonStats = null;

                try
                {
                    seasonStats = await pubgStatsHandler.GetRankedStats(player, statsSeason);
                }
                catch (TooManyRequestsException ex)
                {
                    logger.LogWarning($"Too many requests while requesting stats: {ex}");

                    statsMsg = await pubgStatsHandler.CreateStatsMessage(player, statsSeason, null, ispublic);

                    statsMsg.UserMessage = await ModifyOriginalResponseAsync((msg) =>
                    {
                        msg.Content = "PUBG API limits exceeded. Please try again shortly.";
                        msg.Embed = null;
                        msg.Components = PubgStatsHandler.CreateRefreshButtonComponent(statsMsg.ButtonIdRefresh);
                    });
                    return;
                }

                statsMsg = await pubgStatsHandler.CreateStatsMessage(player, statsSeason, seasonStats, ispublic);
                var seasonStatsEmbed = await pubgStatsHandler.CreatePlayerSeasonStatsEmbed(statsMsg);
                var seasonScrollButtonsComponent = await pubgStatsHandler.CreateSeasonScrollButtonsComponent(statsMsg);

                statsMsg.UserMessage = await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = null;
                    msg.Embed = seasonStatsEmbed;
                    msg.Components = seasonScrollButtonsComponent;
                });
            }
            catch (TooManyRequestsException ex)
            {
                logger.LogWarning($"TooManyRequestsException in SeasonRankedStats: {ex}");
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = "PUBG API limits exceeded. Please try again shortly.";
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"ERROR in SeasonRankedStats: {ex}");
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = "Something went horribly wrong :(";
                });
            }
        }

        [SlashCommand("leaderboard", "")]
        public async Task SeasonLeaderboard(bool ispublic = false, int season = -1)
        {
            logger.LogInformation($"SeasonLeaderboard initiated by {Context.User.Username} for season '{season}'");

            await RespondAsync($"Retrieving season leaderboard..", ephemeral: !ispublic);

            var leaderboardSeason = await seasonRepository.GetSeason(season);

            if (leaderboardSeason == null)
            {
                logger.LogInformation("Could not retrieve season. Leaderboard not posted.");
                await ModifyOriginalResponseAsync((msg) => msg.Content = "Ranked season not found. There might be an issue. Use `refreshseasons` command to refresh season cache.");
                return;
            }

            var lbPlayers = await pubgStatsHandler.GetLeaderboardPlayers(leaderboardSeason.Id, 10);

            if (lbPlayers == null || lbPlayers.Count == 0)
            {
                await ModifyOriginalResponseAsync(msg => msg.Content = "No data available :(");
                return;
            }

            var embed = pubgStatsHandler.CreateLeaderboardEmded(leaderboardSeason, lbPlayers);

            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = null;
                msg.Embed = embed;
            });
        }

        // Refreshes season cache
        [SlashCommand("refreshseasons", "")]
        public async Task RefreshSeasons()
        {
            logger.LogInformation($"Refresh PUBG seasons initiated by {Context.User.Username}");

            await RespondAsync("Refreshing season cache. This might take a while.", ephemeral: true);

            var success = await pubgStatsHandler.RefreshSeasonCache();

            if (success)
            {
                await ModifyOriginalResponseAsync((msg) => msg.Content = $"Season cache refreshed.");
                logger.LogInformation($"Season cache refresh successful");
            }
            else
            {
                await ModifyOriginalResponseAsync((msg) => msg.Content = $"Season cache refresh failed. See logs for details.");
                logger.LogInformation($"Season cache refresh failed");
            }
        }
    }
}