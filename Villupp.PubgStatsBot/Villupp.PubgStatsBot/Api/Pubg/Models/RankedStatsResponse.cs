using System.Text.Json.Serialization;

namespace Villupp.PubgStatsBot.Api.Pubg.Models
{
    public class RankedStatsAttributes
    {
        [JsonPropertyName("rankedGameModeStats")]
        public RankedGameModeStats Stats { get; set; }
    }

    public class RankTier
    {
        [JsonPropertyName("tier")]
        public string Tier { get; set; }

        [JsonPropertyName("subTier")]
        public string SubTier { get; set; }
    }

    public class RankedStats
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public RankedStatsAttributes Attributes { get; set; }
    }

    public class RankedGameModeStats
    {
        [JsonPropertyName("squad-fpp")]
        public GameModeStats SquadFpp { get; set; }
    }

    public class RankedStatsResponse
    {
        [JsonPropertyName("data")]
        public RankedStats Stats { get; set; }
    }

    public class GameModeStats
    {
        [JsonPropertyName("currentTier")]
        public RankTier CurrentTier { get; set; }

        [JsonPropertyName("currentRankPoint")]
        public int CurrentRankPoint { get; set; }

        [JsonPropertyName("bestTier")]
        public RankTier BestTier { get; set; }

        [JsonPropertyName("bestRankPoint")]
        public int BestRankPoint { get; set; }

        [JsonPropertyName("roundsPlayed")]
        public int RoundsPlayed { get; set; }

        [JsonPropertyName("avgRank")]
        public double AvgRank { get; set; }

        [JsonPropertyName("top10Ratio")]
        public double Top10Ratio { get; set; }

        [JsonPropertyName("winRatio")]
        public double WinRatio { get; set; }

        [JsonPropertyName("assists")]
        public int Assists { get; set; }

        [JsonPropertyName("wins")]
        public int Wins { get; set; }

        [JsonPropertyName("kda")]
        public double Kda { get; set; }

        [JsonPropertyName("kills")]
        public int Kills { get; set; }

        [JsonPropertyName("deaths")]
        public int Deaths { get; set; }

        [JsonPropertyName("damageDealt")]
        public double DamageDealt { get; set; }

        [JsonPropertyName("dBNOs")]
        public int DBNOs { get; set; }
    }
}