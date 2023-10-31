using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Villupp.PubgStatsBot.Api.Pubg.Models;
using Villupp.PubgStatsBot.Config;

namespace Villupp.PubgStatsBot.Api.Pubg
{
    public class PubgApiClient : ApiClient
    {
        public PubgApiClient(ILogger<ApiClient> logger, BotSettings botSettings, HttpClient httpClient) : base(logger, botSettings, httpClient)
        {
        }

        public async Task<MatchResponse> GetMatch(Guid matchId)
        {
            logger.LogInformation($"Getting match {matchId}");

            var reqUri = $"matches/{matchId}";

            var httpResponse = await httpClient.GetAsync(reqUri);

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                LogHttpFailure(httpResponse);

                throw new HttpRequestException();
            }

            var matchRes = await httpResponse.Content.ReadFromJsonAsync<MatchResponse>();

            logger.LogInformation($"Got match ID {matchId} from PUBG API");

            return matchRes;
        }

        public async Task<List<Player>> GetPlayers(List<string> playerNames)
        {
            var playerNamesStr = string.Join(',', playerNames);
            var reqUri = $"players?filter[playerNames]={playerNamesStr}";
            var httpResponse = await httpClient.GetAsync(reqUri);

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                LogHttpFailure(httpResponse);

                throw new HttpRequestException();
            }

            var playerRes = await httpResponse.Content.ReadFromJsonAsync<PlayerResponse>();

            return playerRes.Players;
        }

        public async Task<Player> GetPlayer(string playerName)
        {
            var players = await GetPlayers(new List<string>() { playerName });

            if (players != null && players?.Count > 0)
                return players[0];
            else
                return null;
        }

        public async Task<List<SeasonDetails>> GetSeasons()
        {
            logger.LogInformation($"GetSeasons");

            var reqUri = $"seasons";
            var httpResponse = await httpClient.GetAsync(reqUri);

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                LogHttpFailure(httpResponse);

                throw new HttpRequestException();
            }

            var seasonRes = await httpResponse.Content.ReadFromJsonAsync<SeasonResponse>();

            return seasonRes.Seasons;
        }

        public async Task<RankedStats> GetRankedStats(string playerId, string seasonId)
        {
            logger.LogInformation($"GetRankedStats playerId '{playerId}', seasonId: '{seasonId}'");

            if (string.IsNullOrEmpty(playerId)
                || string.IsNullOrEmpty(seasonId))
            {
                logger.LogInformation($"GetRankedStats: missing one or more parameter.");
                return null;
            }

            var reqUri = $"players/{playerId}/seasons/{seasonId}/ranked";
            var httpResponse = await httpClient.GetAsync(reqUri);

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                LogHttpFailure(httpResponse);

                throw new HttpRequestException();
            }

            var rankedStatsRes = await httpResponse.Content.ReadFromJsonAsync<RankedStatsResponse>();

            return rankedStatsRes.Stats;
        }
    }
}