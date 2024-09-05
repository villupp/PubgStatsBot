using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Villupp.PubgStatsBot.Components;
using Villupp.PubgStatsBot.Config;
using Villupp.PubgStatsBot.PubgLeaderboards;

namespace Villupp.PubgStatsBot
{
    public sealed class BotService : IHostedService
    {
        private readonly ILogger logger;
        private readonly PubgStatsBotSettings botSettings;
        private readonly DiscordSocketClient discordSocketClient;
        private readonly CommandService commandService;
        private readonly InteractionService interactionService;
        private readonly IServiceProvider serviceProvider;
        private readonly PubgLeaderboardPoller pubgLeaderboardPoller;
        private readonly ButtonHandler buttonHandler;

        public BotService(
            ILogger<BotService> logger,
            IHostApplicationLifetime appLifetime,
            PubgStatsBotSettings botSettings,
            DiscordSocketClient discordSocketClient,
            CommandService commandService,
            InteractionService interactionService,
            IServiceProvider serviceProvider,
            PubgLeaderboardPoller pubgLeaderboardPoller,
            ButtonHandler buttonHandler
            )
        {
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            this.logger = logger;
            this.botSettings = botSettings;
            this.discordSocketClient = discordSocketClient;
            this.commandService = commandService;
            this.interactionService = interactionService;
            this.serviceProvider = serviceProvider;
            this.pubgLeaderboardPoller = pubgLeaderboardPoller;
            this.buttonHandler = buttonHandler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"{GetType()}.StartAsync");

            discordSocketClient.Log += WriteDiscordLogMessage;

            if (string.IsNullOrEmpty(botSettings.BotToken))
            {
                throw new Exception($"Bot token (BotSettings.BotToken) not set in configuration.");
            }

            await discordSocketClient.LoginAsync(TokenType.Bot, botSettings.BotToken);
            await discordSocketClient.StartAsync();

            discordSocketClient.Ready += async () =>
            {
                logger.LogInformation("Bot is connected and ready");

                var slashCommands = new List<SlashCommandBuilder>
                {
                    new SlashCommandBuilder()
                        .WithName("pubgstats")
                        .WithDescription("PUBG stats related commands.")
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("player")
                            .WithDescription("Get player stats for ranked season (squad FPP).")
                            .WithType(ApplicationCommandOptionType.SubCommand)
                            .AddOption("playername", ApplicationCommandOptionType.String, "Player name (case sensitive)", isRequired: true)
                            .AddOption("season", ApplicationCommandOptionType.Integer, "Ranked season number")
                            .AddOption("ispublic", ApplicationCommandOptionType.Boolean, "Announce stats in public response (true|false)"))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("leaderboard")
                            .WithDescription("Get leaderboard for ranked season (squad FPP).")
                            .WithType(ApplicationCommandOptionType.SubCommand)
                            .AddOption("season", ApplicationCommandOptionType.Integer, "Ranked season number")
                            .AddOption("ispublic", ApplicationCommandOptionType.Boolean, "Announce leaderboard in public response (true|false)"))
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("refreshseasons")
                            .WithDescription("Refreshes season cache. Might take some time to complete.")
                            .WithType(ApplicationCommandOptionType.SubCommand))
                        ,
                };

                try
                {
                    foreach (var slashCmd in slashCommands)
                        await discordSocketClient.Rest.CreateGlobalCommand(slashCmd.Build());
                }
                catch (HttpException ex)
                {
                    logger.LogError($"Error while creating slash commands: {ex}");
                }

                if (botSettings.PubgLeaderboardPollerIsEnabled)
                {
                    logger.LogInformation("Starting PUBG leaderboard polling service");
                    _ = pubgLeaderboardPoller.Start().ConfigureAwait(false);
                }
            };

            discordSocketClient.ButtonExecuted += buttonHandler.OnButtonExecuted;

            await InstallCommands();
        }

        private async Task WriteDiscordLogMessage(LogMessage msg)
        {
            await Task.CompletedTask;
            logger.LogInformation($"Discord client: {msg.ToString()}");
        }

        public async Task InstallCommands()
        {
            logger.LogInformation($"Installing commands");

            discordSocketClient.SlashCommandExecuted += HandleSlashCommand;
            discordSocketClient.AutocompleteExecuted += HandleAutocomplete;

            await commandService.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: serviceProvider);

            await interactionService.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: serviceProvider);
        }

        private async Task HandleAutocomplete(SocketAutocompleteInteraction arg)
        {
            var context = new InteractionContext(discordSocketClient, arg, arg.Channel);
            await interactionService.ExecuteCommandAsync(context, services: serviceProvider);
        }

        private async Task HandleSlashCommand(SocketSlashCommand slashCommand)
        {
            logger.LogInformation($"Executing slash command {slashCommand.CommandName}");

            var context = new SocketInteractionContext(discordSocketClient, slashCommand);

            await interactionService.ExecuteCommandAsync(
                context: context,
                services: serviceProvider);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("StopAsync has been called.");

            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            logger.LogInformation("OnStarted has been called.");
        }

        private void OnStopping()
        {
            logger.LogInformation("OnStopping has been called.");
        }

        private void OnStopped()
        {
            logger.LogInformation("OnStopped has been called.");
        }
    }
}