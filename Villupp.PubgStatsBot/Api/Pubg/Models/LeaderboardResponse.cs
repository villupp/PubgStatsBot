namespace Villupp.PubgStatsBot.Api.Pubg.Models
{
    public class LeaderboardAttributes
    {
        public string shardId { get; set; }
        public string gameMode { get; set; }
        public string seasonId { get; set; }
        public string name { get; set; }
        public int rank { get; set; }
        public Stats stats { get; set; }
    }

    public class Data
    {
        public string type { get; set; }
        public string id { get; set; }
        public LeaderboardAttributes attributes { get; set; }
        public LeaderboardRelationships relationships { get; set; }
    }

    public class Included
    {
        public string type { get; set; }
        public string id { get; set; }
        public LeaderboardAttributes attributes { get; set; }
    }

    public class LeaderboardLinks
    {
        public string self { get; set; }
    }

    public class LeaderboardMeta
    {
    }

    public class LeaderboardPlayers
    {
        public List<Player> data { get; set; }
    }

    public class LeaderboardRelationships
    {
        public LeaderboardPlayers players { get; set; }
    }

    public class LeaderboardResponse
    {
        public Data data { get; set; }
        public List<Included> included { get; set; }
        public LeaderboardLinks links { get; set; }
        public LeaderboardMeta meta { get; set; }
    }

    public class Stats
    {
        public int rankPoints { get; set; }
        public int wins { get; set; }
        public int games { get; set; }
        public int winRatio { get; set; }
        public int averageDamage { get; set; }
        public int kills { get; set; }
        public int killDeathRatio { get; set; }
        public double kda { get; set; }
        public double averageRank { get; set; }
        public string tier { get; set; }
        public string subTier { get; set; }
    }
}