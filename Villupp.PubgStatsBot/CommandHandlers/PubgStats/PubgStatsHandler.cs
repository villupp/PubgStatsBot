using Discord;
using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Api.Pubg;
using Villupp.PubgStatsBot.Api.Pubg.Models;
using Villupp.PubgStatsBot.Common;
using Villupp.PubgStatsBot.Config;
using Villupp.PubgStatsBot.TableStorage;
using Villupp.PubgStatsBot.TableStorage.Models;
using Villupp.PubgStatsBot.TableStorage.Repositories;

namespace Villupp.PubgStatsBot.CommandHandlers.PubgStats
{
    public class PubgStatsHandler
    {
        private const string SEASONID_PREFIX_RANKED_SQUAD_FPP_PC = "division.bro.official.pc-2018-";
        private const string RANKTIER_NAME_MASTER = "Master";
        private ILogger logger;
        private PubgApiClient pubgClient;
        private PubgSeasonRepository seasonRepository;
        private TableStorageService<PubgPlayer> playerTableService;
        private TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService;
        private TableStorageService<PubgSeason> seasonTableService;
        private PubgStatsBotSettings botSettings;

        public PubgStatsHandler(ILogger<PubgStatsHandler> logger,
            PubgApiClient pubgClient,
            PubgSeasonRepository seasonRepository,
            TableStorageService<PubgPlayer> playerTableService,
            TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService,
            TableStorageService<PubgSeason> seasonTableService,
            PubgStatsBotSettings botSettings
            )
        {
            this.logger = logger;
            this.pubgClient = pubgClient;
            this.seasonRepository = seasonRepository;
            this.playerTableService = playerTableService;
            this.botSettings = botSettings;
            this.lbPlayerTableService = lbPlayerTableService;
            this.seasonTableService = seasonTableService;
        }

        public Embed CreatePlayerSeasonStatsEmbed(PubgPlayer player, PubgLeaderboardPlayer lbPlayer, PubgSeason season, RankedStats rankedStats)
        {
            var statsStr = "";
            var seasonNumber = season.Id.Replace(SEASONID_PREFIX_RANKED_SQUAD_FPP_PC, "");
            var titleText = $"PUBG ranked season {seasonNumber} squad FPP stats for player {(string.IsNullOrEmpty(player.DisplayName) ? player.Name : player.DisplayName)}";
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
                bestSubTierStr = $" {PubgRankHelpers.GetSubTierRomanNumeral(stats.BestTier?.SubTier)}";

            if (stats.CurrentTier.Tier != RANKTIER_NAME_MASTER)
            {
                bestTierStr = $" (season high: **{stats.BestTier?.Tier}{bestSubTierStr}**)";
                subTierStr = $" {PubgRankHelpers.GetSubTierRomanNumeral(stats.CurrentTier?.SubTier)}";
            }

            statsStr += $"Rank: **{stats.CurrentTier?.Tier}{subTierStr}**{bestTierStr}";

            if (lbPlayer != null)
                statsStr += $"\nEU leaderboard rank: **#{lbPlayer.Rank}**";

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

        public Embed CreateLeaderboardEmded(PubgSeason season, List<PubgLeaderboardPlayer> lbPlayers)
        {
            var leaderboardStr = "";
            var seasonNumber = season.Id.Replace(SEASONID_PREFIX_RANKED_SQUAD_FPP_PC, "");
            var titleText = $"PUBG ranked season {seasonNumber} top {lbPlayers.Count}";

            lbPlayers = lbPlayers.OrderBy(p => p.Rank).ToList();

            for (var i = 0; i < lbPlayers.Count; i++)
            {
                var lbPlayer = lbPlayers[i];
                // Sub tier not shown for master
                var subTierStr = $"";

                if (lbPlayer.Tier != RANKTIER_NAME_MASTER)
                    subTierStr = $" {PubgRankHelpers.GetSubTierRomanNumeral(lbPlayer.SubTier)}";

                leaderboardStr += $"\n **#{lbPlayer.Rank}** **[{lbPlayer.Name}](https://pubg.op.gg/user/{lbPlayer.Name})**" +
                        $" [**{lbPlayer.Tier}{subTierStr}**]" +
                        $" [RP: **{lbPlayer.Rp}**]" +
                        $" [Matches: **{lbPlayer.GameCount}**]" +
                        //$" [Wins: **{lbPlayer.WinCount}** (**{string.Format("{0:0.0#}", lbPlayer.WinRatio * 100)}%**)]" +
                        $" [Avg dmg: **{lbPlayer.AvgDamage.Value}**]" +
                        //$" [KDA: **{string.Format("{0:0.0#}", lbPlayer.KdaRatio)}**]" +
                        "";
            }

            var embedBuilder = new EmbedBuilder()
                 .WithTitle(titleText)
                 .WithDescription(leaderboardStr)
                 .WithColor(Color.Blue)
                 ;

            return embedBuilder.Build();
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

            seasonRepository.FlushCache();
            return true;
        }

        public async Task<PubgPlayer> GetPlayer(string playerName)
        {
            var players = await playerTableService.Get(p => p.Name == playerName.ToLower());

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
                Name = player.Attributes.Name.ToLower(),
                DisplayName = player.Attributes.Name
            };

            await playerTableService.Add(pubgPlayer);
            return pubgPlayer;
        }

        public async Task<PubgLeaderboardPlayer> GetLeaderboardPlayer(string playerName, string season)
        {
            var lbPlayers = await lbPlayerTableService.Get(p => p.Name == playerName && p.Season == season);
            return lbPlayers.Count > 0 ? lbPlayers[0] : null;
        }

        public async Task<List<PubgLeaderboardPlayer>> GetLeaderboardPlayers(string season, int count = 500)
        {
            if (count > 500)
            {
                logger.LogWarning($"Tried to query for nore than 500 leaderboard players..");
                count = 500;
            }

            return await lbPlayerTableService.Get(p => p.Season == season && p.Rank <= count);
        }

        public async Task<RankedStats> GetRankedStats(PubgPlayer player, PubgSeason season)
        {
            logger.LogInformation($"GetRankedStats '{player.Name}', season '{season.Id}'");

            var stats = await pubgClient.GetRankedStats(player.Id, season.Id);
            return stats;
        }
    }
}