using System.Text.Json.Serialization;

namespace Villupp.PubgStatsBot.Api.Pubg.Models
{
    public class PlayerResponse
    {
        [JsonPropertyName("data")]
        public List<Player> Players { get; set; }

        [JsonPropertyName("links")]
        public Links Links { get; set; }

        [JsonPropertyName("meta")]
        public Meta Meta { get; set; }
    }

    public class Attributes
    {
        [JsonPropertyName("patchVersion")]
        public string PatchVersion { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("stats")]
        public object Stats { get; set; }

        [JsonPropertyName("titleId")]
        public string TitleId { get; set; }

        [JsonPropertyName("shardId")]
        public string ShardId { get; set; }
    }

    public class Player
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public Attributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public Relationships Relationships { get; set; }

        [JsonPropertyName("links")]
        public Links Links { get; set; }
    }

    public class Links
    {
        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("schema")]
        public string Schema { get; set; }
    }

    public class Matches
    {
        [JsonPropertyName("data")]
        public List<PlayerMatch> Data { get; set; }
    }

    public class PlayerMatch
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }

    public class Meta
    {
    }

    public class Relationships
    {
        [JsonPropertyName("matches")]
        public Matches Matches { get; set; }
    }
}