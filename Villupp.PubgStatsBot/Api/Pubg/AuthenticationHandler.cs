using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Villupp.PubgStatsBot.Config;

namespace Villupp.PubgStatsBot.Api.Pubg
{
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly PubgStatsBotSettings settings;
        private readonly ILogger<AuthenticationHandler> logger;

        public AuthenticationHandler(PubgStatsBotSettings settings, ILogger<AuthenticationHandler> logger)
        {
            this.logger = logger;
            this.settings = settings;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.PubgApiKey);
            request.Headers.Add("Accept", "application/vnd.api+json");

            return await base.SendAsync(request, cancellationToken);
        }
    }
}