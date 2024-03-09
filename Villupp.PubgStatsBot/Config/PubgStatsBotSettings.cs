namespace Villupp.PubgStatsBot.Config
{
    public class PubgStatsBotSettings
    {
        public string AppInsightsConnectionString { get; set; } = null;
        public string BotToken { get; set; }
        public string StorageKey { get; set; }
        public string ApplicationInsightsKey { get; set; }
        public string PubgApiBaseUrl { get; set; }
        public bool PubgLeaderboardPollerIsEnabled { get; set; } = true;
        public int PubgLeaderboardPollingIntervalMinutes { get; set; } = 120;
        public string PubgApiKey { get; set; }
        public string PubgStatsRankImageTemplateUrl { get; set; } = "";
    }
}