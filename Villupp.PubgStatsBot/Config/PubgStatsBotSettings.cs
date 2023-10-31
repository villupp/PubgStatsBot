namespace Villupp.PubgStatsBot.Config
{
    public class PubgStatsBotSettings
    {
        public string BotToken { get; set; }
        public string StorageKey { get; set; }
        public string ApplicationInsightsKey { get; set; }
        public string PubgApiBaseUrl { get; set; }
        public string PubgApiKey { get; set; }
        public string PubgStatsRankImageTemplateUrl { get; set; } = "";
    }
}