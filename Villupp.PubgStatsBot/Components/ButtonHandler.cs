using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.CommandHandlers.PubgStats;

public class ButtonHandler
    {
        private ILogger logger;
        private PubgStatsHandler pubgStatsHandler;

        public ButtonHandler(ILogger<ButtonHandler> logger, PubgStatsHandler pubgStatsHandler)
        {
            this.logger = logger;
            this.pubgStatsHandler = pubgStatsHandler;
        }

        public async Task OnButtonExecuted(SocketMessageComponent component)
        {
            logger.LogInformation($"Button ID '{component.Data.CustomId}' was selected by {component.User.Username}");

            if (!Guid.TryParse(component.Data.CustomId, out var buttonCustomId))
                return;

            if (pubgStatsHandler.StatsMessages.Any(msg => msg.ButtonIdPreviousSeason == buttonCustomId))
                await pubgStatsHandler.OnPreviousSeasonButtonSelect(buttonCustomId, component);
            else if (pubgStatsHandler.StatsMessages.Any(msg => msg.ButtonIdNextSeason == buttonCustomId))
                await pubgStatsHandler.OnNextSeasonButtonSelect(buttonCustomId, component);
            else
                await component.DeferAsync();
        }
    }