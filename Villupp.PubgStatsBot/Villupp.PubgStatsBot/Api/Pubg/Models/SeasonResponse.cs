using System.Text.Json.Serialization;

namespace Villupp.PubgStatsBot.Api.Pubg.Models
{
    public class SeasonAttributes
    {
        [JsonPropertyName("isCurrentSeason")]
        public bool IsCurrentSeason { get; set; }

        [JsonPropertyName("isOffseason")]
        public bool IsOffseason { get; set; }
    }

    public class SeasonDetails
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public SeasonAttributes Attributes { get; set; }
    }

    public class SeasonResponse
    {
        [JsonPropertyName("data")]
        public List<SeasonDetails> Seasons { get; set; }
    }
}