using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Config;

namespace Villupp.PubgStatsBot.Api
{
    public abstract class ApiClient
    {
        protected readonly ILogger<ApiClient> logger;
        protected readonly BotSettings botSettings;
        protected readonly HttpClient httpClient;

        public ApiClient(
            ILogger<ApiClient> logger,
            BotSettings botSettings,
            HttpClient httpClient)
        {
            this.logger = logger;
            this.botSettings = botSettings;
            this.httpClient = httpClient;
        }

        protected async void LogHttpFailure(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError(
                $"Http request {response.RequestMessage.Method.Method} {response.RequestMessage.RequestUri} " +
                $"returned status {response.StatusCode} with response content: {content}"
            );
        }
    }
}