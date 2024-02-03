using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.CommandHandlers.PubgStats;

namespace Villupp.PubgStatsBot.Modules
{
    [Group("pubgstats", "")]
    public class PubgStatsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogger logger;
        private PubgStatsHandler pubgStatsHandler;

        public PubgStatsModule(ILogger<PubgStatsModule> logger, PubgStatsHandler pubgStatsHandler)
        {
            this.logger = logger;
            this.pubgStatsHandler = pubgStatsHandler;
        }

        // Posts PUBG player stats for current ongoing season
        [SlashCommand("player", "")]
        public async Task CurrentSeasonStats(string playername, bool ispublic = false, int season = -1)
        {
            logger.LogInformation($"CurrentSeasonStats initiated by {Context.User.Username} for player '{playername}'");

            if (string.IsNullOrEmpty(playername))
            {
                await RespondAsync("Provide a player name.", ephemeral: true);
                return;
            }

            await RespondAsync($"Retrieving stats for {playername} ...", ephemeral: !ispublic);

            var player = await pubgStatsHandler.GetPlayer(playername);
            var statsSeason = await pubgStatsHandler.GetSeason(season);

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

            var seasonStats = await pubgStatsHandler.GetRankedStats(player, statsSeason);
            var lbPlayer = await pubgStatsHandler.GetLeaderboardPlayer(player.DisplayName, statsSeason.Id);
            var embed = pubgStatsHandler.CreateStatsEmded(player, lbPlayer, statsSeason, seasonStats);

            await ModifyOriginalResponseAsync((msg) =>
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