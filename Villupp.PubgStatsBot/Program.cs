using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using Villupp.PubgStatsBot.Api.Pubg;
using Villupp.PubgStatsBot.CommandHandlers.PubgStats;
using Villupp.PubgStatsBot.Components;
using Villupp.PubgStatsBot.Config;
using Villupp.PubgStatsBot.PubgLeaderboards;
using Villupp.PubgStatsBot.Services;
using Villupp.PubgStatsBot.TableStorage;
using Villupp.PubgStatsBot.TableStorage.Models;
using Villupp.PubgStatsBot.TableStorage.Repositories;

namespace Villupp.PubgStatsBot
{
    internal class Program
    {
        private static PubgStatsBotSettings botSettings;

        private static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
            {
                var settings = new PubgStatsBotSettings();
                hostContext.Configuration.Bind(nameof(PubgStatsBotSettings), settings);
                botSettings = settings;

                var discordSocketConfig = new DiscordSocketConfig()
                {
                };

                var commandServiceConfig = new CommandServiceConfig()
                {
                    CaseSensitiveCommands = false
                };

                var interactionServiceConfig = new InteractionServiceConfig()
                {
                };

                var discordRestConfig = new DiscordRestConfig() {

                };

                services.AddAzureClients(builder =>
                {
                    builder.AddTableServiceClient(botSettings.StorageKey);
                });

                services.AddMemoryCache();

                services.AddHostedService<BotService>();

                services.AddSingleton(serviceProvider => serviceProvider);
                services.AddSingleton(botSettings);
                services.AddSingleton(discordSocketConfig);
                services.AddSingleton(commandServiceConfig);
                services.AddSingleton(interactionServiceConfig);
                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton<DiscordRestClient>();
                services.AddSingleton<CommandService>();
                services.AddSingleton<InteractionService>();
                services.AddSingleton<TableStorageService<PubgSeason>>();
                services.AddSingleton<TableStorageService<PubgPlayer>>();
                services.AddSingleton<TableStorageService<PubgLeaderboardPlayer>>();
                services.AddSingleton<PubgSeasonRepository>();
                services.AddSingleton<PubgStatsHandler>();
                services.AddSingleton<PubgLeaderboardPoller>();
                services.AddSingleton<AuthenticationHandler>();
                services.AddSingleton<ButtonHandler>();
                services.AddSingleton<PubgPlayerService>();

                services.AddHttpClient<PubgApiClient>(client => { client.BaseAddress = new Uri(botSettings.PubgApiBaseUrl); })
                    .AddHttpMessageHandler<AuthenticationHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                    {
                        DefaultProxyCredentials = CredentialCache.DefaultCredentials
                    })
                    ;
            })
            .ConfigureLogging((context, builder) =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                if (!string.IsNullOrEmpty(botSettings.AppInsightsConnectionString))
                    builder.AddApplicationInsights(
                        configureTelemetryConfiguration: (config) => config.ConnectionString = botSettings.AppInsightsConnectionString, 
                        configureApplicationInsightsLoggerOptions: (config) => { });
            })
            .Build();

            host.Run();
        }
    }
}