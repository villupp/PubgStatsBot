using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Api;
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
        private const string RANKTIER_NAME_MASTER = "Master";
        private ILogger logger;
        private PubgApiClient pubgClient;
        private PubgSeasonRepository seasonRepository;
        private TableStorageService<PubgPlayer> playerTableService;
        private TableStorageService<PubgLeaderboardPlayer> lbPlayerTableService;
        private TableStorageService<PubgSeason> seasonTableService;
        private PubgStatsBotSettings botSettings;

        public List<PubgStatsMessage> StatsMessages { get; set; }

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

            StatsMessages = [];
        }

        public async Task<PubgStatsMessage> CreateStatsMessage(PubgPlayer player, PubgSeason season, RankedStats stats, bool isPublic)
        {
            logger.LogInformation($"Creating new stats message for" +
                $", player '{player.DisplayName}'" +
                $", season: '{season.Id}'");

            var statsMessage = new PubgStatsMessage()
            {
                Player = player,
                SelectedSeason = season,
                IsPublic = isPublic
            };

            if (stats != null)
                statsMessage.RankedSeasonStats[season.SeasonNumber] = stats;

            if (StatsMessages.Count > 100)
            {
                await DeleteStatsMessage(StatsMessages[0]);
                StatsMessages.RemoveAt(0);
            }

            StatsMessages.Add(statsMessage);

            return statsMessage;
        }

        private async Task DeleteStatsMessage(PubgStatsMessage statsMsg)
        {
            logger.LogInformation($"DeleteStatsMessage {statsMsg?.Id}");

            try
            {
                if (statsMsg?.UserMessage != null)
                {
                    var message = statsMsg.UserMessage;
                    logger.LogInformation($"Deleting message ID {message?.Id}");
                    await message.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"DeleteMessage failed for stats message ID {statsMsg?.UserMessage?.Id}: {ex.Message}");
            }
        }

        public async Task<Embed> CreatePlayerSeasonStatsEmbed(PubgStatsMessage statsMsg)
        {
            var statsStr = "";
            var titleText = $"PUBG ranked season {statsMsg.SelectedSeason.SeasonNumber} squad FPP stats for player " +
                $"{(string.IsNullOrEmpty(statsMsg.Player.DisplayName) ? statsMsg.Player.Name : statsMsg.Player.DisplayName)}";
            var pubgOpGgUrl = $"https://pubg.op.gg/user/{statsMsg.Player.Name}";
            var seasonStats = statsMsg.RankedSeasonStats.GetValueOrDefault(statsMsg.SelectedSeason.SeasonNumber);
            var lbPlayer = await GetLeaderboardPlayer(statsMsg.Player.DisplayName, statsMsg.SelectedSeason.Id);

            if (seasonStats?.Attributes?.Stats?.SquadFpp == null)
            {
                return new EmbedBuilder()
                 .WithTitle(titleText)
                 .WithDescription($"No stats found :(")
                 .WithColor(Color.DarkGrey)
                 .WithUrl(pubgOpGgUrl)
                 .Build();
            }
            var stats = seasonStats.Attributes.Stats.SquadFpp;
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
            {
                var region = lbPlayer.Region.Replace("pc-", "").ToUpper();
                statsStr += $"\nLeaderboard ({region}) rank: **#{lbPlayer.Rank}**";
            }

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

        public static Embed CreateLeaderboardEmded(string region, PubgSeason season, List<PubgLeaderboardPlayer> lbPlayers)
        {
            var leaderboardStr = "";
            var dispRegion = region.Replace("pc-", "").ToUpper();
            var titleText = $"PUBG ranked {dispRegion} season {season.SeasonNumber} top {lbPlayers.Count}";

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

        public async Task<MessageComponent> CreateSeasonScrollButtonsComponent(PubgStatsMessage statsMsg)
        {
            if (statsMsg.IsPublic)
                return null;

            var currentSeason = await seasonRepository.GetCurrentSeason();
            var isPreviousSeasonAvailable = await seasonRepository.GetSeason(statsMsg.SelectedSeason.SeasonNumber - 1) != null
                && statsMsg.SelectedSeason.SeasonNumber > 7;
            var isNextSeasonAvailable = currentSeason.Id != statsMsg.SelectedSeason.Id;

            var btnCompBuilder = new ComponentBuilder();
            btnCompBuilder.WithButton("◀", statsMsg.ButtonIdPreviousSeason.ToString(), ButtonStyle.Primary, disabled: !isPreviousSeasonAvailable);
            btnCompBuilder.WithButton("▶", statsMsg.ButtonIdNextSeason.ToString(), ButtonStyle.Primary, disabled: !isNextSeasonAvailable);

            return btnCompBuilder.Build();
        }

        private PubgStatsMessage GetStatsMessage(Guid buttonId)
        {
            return StatsMessages.Where(rs =>
                rs.ButtonIdPreviousSeason == buttonId
                || rs.ButtonIdNextSeason == buttonId
                || rs.ButtonIdRefresh == buttonId
                )
                .FirstOrDefault();
        }

        public async Task OnNextSeasonButtonSelect(Guid btnId, SocketMessageComponent msgComponent)
        {
            var statsMsg = GetStatsMessage(btnId);
            var statsSeason = await seasonRepository.GetSeason(statsMsg.SelectedSeason.SeasonNumber + 1);

            await UpdateStatsMessage(statsMsg, statsSeason, msgComponent);
        }

        public async Task OnPreviousSeasonButtonSelect(Guid btnId, SocketMessageComponent msgComponent)
        {
            var statsMsg = GetStatsMessage(btnId);
            var statsSeason = await seasonRepository.GetSeason(statsMsg.SelectedSeason.SeasonNumber - 1);

            await UpdateStatsMessage(statsMsg, statsSeason, msgComponent);
        }

        public async Task OnRefreshButtonSelect(Guid btnId, SocketMessageComponent msgComponent)
        {
            var statsMsg = GetStatsMessage(btnId);

            await UpdateStatsMessage(statsMsg, statsMsg.SelectedSeason, msgComponent);
        }

        public async Task UpdateStatsMessage(PubgStatsMessage statsMsg, PubgSeason season, SocketMessageComponent msgComponent)
        {
            try
            {
                RankedStats seasonStats = null;

                statsMsg.SelectedSeason = season;

                if (!statsMsg.RankedSeasonStats.ContainsKey(season.SeasonNumber))
                {
                    try
                    {
                        seasonStats = await GetRankedStats(statsMsg.Player, season);
                    }
                    catch (TooManyRequestsException ex)
                    {
                        logger.LogWarning($"Too many requests while requesting stats: {ex}");
                        await HandleStatsMessageError(statsMsg, "PUBG API limits exceeded. Please try again shortly.", msgComponent);
                        return;
                    }

                    statsMsg.RankedSeasonStats[season.SeasonNumber] = seasonStats;
                }

                var embed = await CreatePlayerSeasonStatsEmbed(statsMsg);
                var buttonsComponent = await CreateSeasonScrollButtonsComponent(statsMsg);

                await msgComponent.UpdateAsync(mp =>
                {
                    mp.Content = null;
                    mp.Embed = embed;
                    mp.Components = buttonsComponent;
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"ERROR in UpdateStatsMessage: {ex}");
                await HandleStatsMessageError(statsMsg, "Something went wrong. Please try again shortly.", msgComponent);
            }
        }

        public async Task HandleStatsMessageError(PubgStatsMessage statsMsg, string errorMessage, SocketMessageComponent msgComponent)
        {
            var refreshButtonComponent = CreateRefreshButtonComponent(statsMsg.ButtonIdRefresh);

            await msgComponent.UpdateAsync(mp =>
            {
                mp.Content = errorMessage;
                mp.Embed = null;
                mp.Components = refreshButtonComponent;
            });
        }

        public static MessageComponent CreateRefreshButtonComponent(Guid btnId)
        {
            return new ComponentBuilder()
                .WithButton("↻", btnId.ToString(), ButtonStyle.Success)
                .Build();
        }

        private string GetRankThumbnailUrl(RankTier rankTier)
        {
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

            if (!await seasonTableService.DeleteAll())
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

            logger.LogInformation($"Player not found by name '{playerName}'. Requesting from API..");
            
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
            logger.LogInformation($"Added player '{playerName}' to storage cache.");
            
            return pubgPlayer;
        }

        public async Task<PubgLeaderboardPlayer> GetLeaderboardPlayer(string playerName, string season)
        {
            var lbPlayers = await lbPlayerTableService.Get(p => p.Name == playerName && p.Season == season);
            return lbPlayers.Count > 0 ? lbPlayers[0] : null;
        }

        public async Task<List<PubgLeaderboardPlayer>> GetLeaderboardPlayers(string region, string season, int count = 500)
        {
            if (count > 500)
            {
                logger.LogWarning($"Tried to query for more than 500 leaderboard players..");
                count = 500;
            }

            return await lbPlayerTableService.Get(p => p.Region == region && p.Season == season && p.Rank <= count);
        }

        public async Task<RankedStats> GetRankedStats(PubgPlayer player, PubgSeason season)
        {
            logger.LogInformation($"GetRankedStats '{player.Name}', season '{season.Id}'");

            var stats = await pubgClient.GetRankedStats(player.Id, season.Id);
            return stats;
        }
    }
}
