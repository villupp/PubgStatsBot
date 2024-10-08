
using Discord;
using Villupp.PubgStatsBot.Api.Pubg.Models;
using Villupp.PubgStatsBot.TableStorage.Models;

public class PubgStatsMessage
{
  public PubgStatsMessage()
  {
    RankedSeasonStats = [];
    ButtonIdNextSeason = Guid.NewGuid();
    ButtonIdPreviousSeason = Guid.NewGuid();
    ButtonIdRefresh = Guid.NewGuid();
  }

  public Guid Id { get; set; }
  public Guid ButtonIdPreviousSeason { get; set; }
  public Guid ButtonIdNextSeason { get; set; }
  public Guid ButtonIdRefresh { get; set; }
  public PubgPlayer Player { get; set; }
  public PubgSeason SelectedSeason { get; set; }
  // <SeasonNumber, RankedStats>
  public Dictionary<int, RankedStats> RankedSeasonStats { get; set; }
  public bool IsPublic { get; set; }
  public IUserMessage UserMessage { get; set; }
}