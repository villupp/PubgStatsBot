﻿using Azure;
using Azure.Data.Tables;

namespace Villupp.PubgStatsBot.TableStorage.Models
{
    public class PubgSeason : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public bool IsCurrentSeason { get; set; }
        public bool IsOffSeason { get; set; }
        public string Id { get; set; }
    }
}