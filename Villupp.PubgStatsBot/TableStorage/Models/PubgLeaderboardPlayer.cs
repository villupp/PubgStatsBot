using Azure;
using Azure.Data.Tables;

namespace Villupp.PubgStatsBot.TableStorage.Models
{
    public class PubgLeaderboardPlayer : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Name { get; set; }
        public int? Rank { get; set; }
        public string Id { get; set; }
        public string Season { get; set; }
        public int? Rp { get; set; }
        public int? GameCount { get; set; }
        public int? WinCount { get; set; }
        public decimal? WinRatio { get; set; }
        public int? AvgDamage { get; set; }
        //public decimal? KdRatio { get; set; }
        public decimal? KdaRatio { get; set; }
        public string Tier { get; set; }
        public string SubTier { get; set; }
        public string Region { get; set; }
    }
}