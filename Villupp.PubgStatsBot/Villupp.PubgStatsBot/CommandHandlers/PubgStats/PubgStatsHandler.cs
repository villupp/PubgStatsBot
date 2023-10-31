using Discord;
using Microsoft.Extensions.Logging;
using OfBot.TableStorage.Models;
using Villupp.PubgStatsBot.Api.Pubg;
using Villupp.PubgStatsBot.Api.Pubg.Models;
using Villupp.PubgStatsBot.Config;
using Villupp.PubgStatsBot.TableStorage;

namespace Villupp.PubgStatsBot.CommandHandlers.PubgStats
{
    public class PubgStatsHandler
    {
        private const string SEASONID_PREFIX_RANKED_SQUAD_FPP_PC = "division.bro.official.pc-2018-";
        private const string RANKTIER_NAME_MASTER = "Master";
        private ILogger logger;
        private PubgApiClient pubgClient;
        private TableStorageService<PubgSeason> seasonTableService;
        private TableStorageService<PubgPlayer> playerTableService;
        private BotSettings botSettings;

        public List<PubgSeason> Seasons { get; set; }

        public PubgStatsHandler(ILogger<PubgStatsHandler> logger,
            PubgApiClient pubgClient,
            TableStorageService<PubgSeason> seasonTableService,
            TableStorageService<PubgPlayer> playerTableService,
            BotSettings botSettings
            )
        {
            this.logger = logger;
            this.pubgClient = pubgClient;
            this.seasonTableService = seasonTableService;
            this.playerTableService = playerTableService;
            this.botSettings = botSettings;

            PopulateSeasons().Wait();
        }

        public Embed CreateStatsEmded(PubgPlayer player, PubgSeason season, RankedStats rankedStats)
        {
            var statsStr = "";
            var seasonNumber = season.Id.Replace(SEASONID_PREFIX_RANKED_SQUAD_FPP_PC, "");
            var titleText = $"PUBG ranked season {seasonNumber} squad FPP stats for player {player.Name}";
            var pubgOpGgUrl = $"https://pubg.op.gg/user/{player.Name}";

            if (rankedStats?.Attributes?.Stats?.SquadFpp == null)
            {
                return new EmbedBuilder()
                 .WithTitle(titleText)
                 .WithDescription($"No stats found :(")
                 .WithColor(Color.DarkGrey)
                 .WithUrl(pubgOpGgUrl)
                 .Build();
            }
            var stats = rankedStats.Attributes.Stats.SquadFpp;
            var kdr = 0.00m;
            var kdrDisplay = "N/A";

            if (stats.Deaths > 0)
            {
                kdr = stats.Kills / (decimal)stats.Deaths;
                kdrDisplay = string.Format("{0:0.0#}", kdr);
            }

            // Sub tier not shown for master
            var subTierStr = "";
            var bestSubTierStr = "";
            var bestTierStr = "";

            if (stats.BestTier.Tier != RANKTIER_NAME_MASTER)
                bestSubTierStr = $" {GetSubTierRomanNumeral(stats.BestTier?.SubTier)}";

            if (stats.CurrentTier.Tier != RANKTIER_NAME_MASTER)
            {
                bestTierStr = $" (season high: **{stats.BestTier?.Tier}{bestSubTierStr}**)";
                subTierStr = $" {GetSubTierRomanNumeral(stats.CurrentTier?.SubTier)}";
            }

            statsStr += $"Rank: **{stats.CurrentTier?.Tier}{subTierStr}**{bestTierStr}";
            statsStr += $"\nRP: **{stats.CurrentRankPoint}** (season high: **{stats.BestRankPoint}**)";
            statsStr += $"\nMatches: **{stats.RoundsPlayed}** Wins: **{stats.Wins}** (**{string.Format("{0:0.0#}", stats.WinRatio * 100)}%**)";
            statsStr += $"\nAvg placement: **#{string.Format("{0:0.0#}", stats.AvgRank)}** Top 10: **{string.Format("{0:0.0#}", stats.Top10Ratio * 100)}%**";
            statsStr += $"\nKDR: **{kdrDisplay}** KDA: **{string.Format("{0:0.0#}", stats.Kda)}** Avg dmg: **{string.Format("{0:0}", stats.DamageDealt / stats.RoundsPlayed)}**";

            var embedBuilder = new EmbedBuilder()
                 .WithTitle(titleText)
                 .WithDescription(statsStr)
                 .WithColor(Color.Blue)
                 .WithUrl(pubgOpGgUrl)
                 .WithThumbnailUrl(GetRankThumbnailUrl(stats.CurrentTier))
                 ;

            return embedBuilder.Build();
        }

        private static string GetSubTierRomanNumeral(string subTier)
        {
            return subTier switch
            {
                "1" => "I",
                "2" => "II",
                "3" => "III",
                "4" => "IV",
                "5" => "V",
                _ => "",
            };
        }

        private string GetRankThumbnailUrl(RankTier rankTier)
        {
            //https://opgg-pubg-static.akamaized.net/images/tier/competitive/Platinum-5.png
            if (rankTier == null || string.IsNullOrEmpty(botSettings.PubgStatsRankImageTemplateUrl))
                return "";

            if (string.IsNullOrEmpty(rankTier.Tier) || string.IsNullOrEmpty(rankTier.SubTier))
                return "";

            return botSettings.PubgStatsRankImageTemplateUrl.Replace("{RANK}", $"{rankTier.Tier}-{rankTier.SubTier}");
        }

        private async Task PopulateSeasons()
        {
            Seasons = await seasonTableService.Get();
        }

        public async Task<bool> RefreshSeasonCache()
        {
            var seasons = await pubgClient.GetSeasons();

            if (seasons == null || seasons.Count == 0)
                return false;

            if (!(await seasonTableService.DeleteAll()))
            {
                logger.LogError($"RefreshSeasonCache: table clear failed.");
                return false;
            }

            foreach (var season in seasons)
            {
                await seasonTableService.Add(new PubgSeason
                {
                    PartitionKey = "",
                    RowKey = Guid.NewGuid().ToString(),
                    Id = season.Id,
                    IsCurrentSeason = season.Attributes.IsCurrentSeason,
                    IsOffSeason = season.Attributes.IsOffseason
                });
            }

            await PopulateSeasons();
            return true;
        }

        public async Task<PubgPlayer> GetPlayer(string playerName)
        {
            var players = await playerTableService.Get(p => p.Name == playerName);

            if (players != null && players.Count > 0)
                return players[0];

            // If not found --> retrieve from API
            var player = await pubgClient.GetPlayer(playerName);

            if (player?.Attributes == null)
                return null;

            var pubgPlayer = new PubgPlayer()
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "",
                Id = player.Id,
                Name = player.Attributes.Name
            };

            await playerTableService.Add(pubgPlayer);
            return pubgPlayer;
        }

        public async Task<PubgSeason> GetSeason(int seasonNumber = -1)
        {
            if (Seasons == null || Seasons.Count == 0)
                await PopulateSeasons();

            if (seasonNumber == -1)
                // Current season
                return Seasons.Where(s => s.IsCurrentSeason && s.Id.StartsWith(SEASONID_PREFIX_RANKED_SQUAD_FPP_PC)).FirstOrDefault();
            else
                return Seasons.Where(s => s.Id == $"{SEASONID_PREFIX_RANKED_SQUAD_FPP_PC}{seasonNumber}").FirstOrDefault();
        }

        public async Task<RankedStats> GetRankedStats(PubgPlayer player, PubgSeason season)
        {
            logger.LogInformation($"GetRankedStats '{player.Name}', season '{season.Id}'");

            var stats = await pubgClient.GetRankedStats(player.Id, season.Id);
            return stats;
        }
    }
}