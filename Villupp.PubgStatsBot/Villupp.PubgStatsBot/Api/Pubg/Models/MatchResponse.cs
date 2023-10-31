using System.Text.Json.Serialization;

namespace Villupp.PubgStatsBot.Api.Pubg.Models
{
    public class MatchAttributes
    {
        [JsonPropertyName("titleId")]
        public string TitleId { get; set; }

        [JsonPropertyName("shardId")]
        public string ShardId { get; set; }

        [JsonPropertyName("tags")]
        public object Tags { get; set; }

        [JsonPropertyName("mapName")]
        public string MapName { get; set; }

        [JsonPropertyName("isCustomMatch")]
        public bool IsCustomMatch { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("matchType")]
        public string MatchType { get; set; }

        [JsonPropertyName("seasonState")]
        public string SeasonState { get; set; }

        [JsonPropertyName("gameMode")]
        public string GameMode { get; set; }

        [JsonPropertyName("won")]
        public string Won { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("URL")]
        public string URL { get; set; }
    }

    public class MatchPlayerAttributes
    {
        [JsonPropertyName("actor")]
        public string Actor { get; set; }

        [JsonPropertyName("shardId")]
        public string ShardId { get; set; }

        [JsonPropertyName("stats")]
        public MatchPlayerStats Stats { get; set; }
    }

    public class MatchDetails
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("attributes")]
        public MatchAttributes Attributes { get; set; }
    }

    public class MatchPlayer
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public MatchPlayerAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public Relationships Relationships { get; set; }
    }

    public class MatchResponse
    {
        [JsonPropertyName("data")]
        public MatchDetails Match { get; set; }

        [JsonPropertyName("included")]
        public List<MatchPlayer> Players { get; set; }
    }

    public class MatchPlayerStats
    {
        [JsonPropertyName("DBNOs")]
        public int DBNOs { get; set; }

        [JsonPropertyName("assists")]
        public int Assists { get; set; }

        [JsonPropertyName("boosts")]
        public int Boosts { get; set; }

        [JsonPropertyName("damageDealt")]
        public double DamageDealt { get; set; }

        [JsonPropertyName("deathType")]
        public string DeathType { get; set; }

        [JsonPropertyName("headshotKills")]
        public int HeadshotKills { get; set; }

        [JsonPropertyName("heals")]
        public int Heals { get; set; }

        [JsonPropertyName("killPlace")]
        public int KillPlace { get; set; }

        [JsonPropertyName("killStreaks")]
        public int KillStreaks { get; set; }

        [JsonPropertyName("kills")]
        public int Kills { get; set; }

        [JsonPropertyName("longestKill")]
        public double LongestKill { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("playerId")]
        public string PlayerId { get; set; }

        [JsonPropertyName("revives")]
        public int Revives { get; set; }

        [JsonPropertyName("rideDistance")]
        public double RideDistance { get; set; }

        [JsonPropertyName("roadKills")]
        public int RoadKills { get; set; }

        [JsonPropertyName("swimDistance")]
        public decimal SwimDistance { get; set; }

        [JsonPropertyName("teamKills")]
        public int TeamKills { get; set; }

        [JsonPropertyName("timeSurvived")]
        public decimal? TimeSurvived { get; set; }

        [JsonPropertyName("vehicleDestroys")]
        public int VehicleDestroys { get; set; }

        [JsonPropertyName("walkDistance")]
        public double WalkDistance { get; set; }

        [JsonPropertyName("weaponsAcquired")]
        public int WeaponsAcquired { get; set; }

        [JsonPropertyName("winPlace")]
        public int WinPlace { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        [JsonPropertyName("teamId")]
        public int? TeamId { get; set; }
    }
}