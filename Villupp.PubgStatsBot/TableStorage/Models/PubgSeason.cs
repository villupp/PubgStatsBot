using Azure;
using Azure.Data.Tables;

namespace Villupp.PubgStatsBot.TableStorage.Models
{
    public class PubgSeason : ITableEntity
    {
        private const string SEASONID_PREFIX_RANKED_SQUAD_FPP_PC = "division.bro.official.pc-2018-";

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public bool IsCurrentSeason { get; set; }
        public bool IsOffSeason { get; set; }
        public string Id { get; set; }
        public int SeasonNumber => int.Parse(Id.Replace(SEASONID_PREFIX_RANKED_SQUAD_FPP_PC, "")); 
    }
}