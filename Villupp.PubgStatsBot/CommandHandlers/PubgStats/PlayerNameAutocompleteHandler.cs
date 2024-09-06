using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Villupp.PubgStatsBot.TableStorage;
using Villupp.PubgStatsBot.TableStorage.Models;

namespace Villupp.PubgStatsBot.CommandHandlers.PubgStats;

public class PlayerNameAutocompleteHandler(
  TableStorageService<PubgPlayer> playerTableService) : AutocompleteHandler
{
  private readonly TableStorageService<PubgPlayer> playerTableService = playerTableService;

  public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
    IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
  {
    var interaction = (SocketAutocompleteInteraction)autocompleteInteraction;
    var playerNameOpt = interaction.Data.Options.Where(opt => opt.Name == "playername").FirstOrDefault();

    if (playerNameOpt == null)
      return AutocompletionResult.FromSuccess();

    var playerNameInput = playerNameOpt.Value.ToString().ToLower();
    var upperBound = NextPrefix(playerNameInput);

    if (playerNameInput.Length < 1)
      return AutocompletionResult.FromSuccess([]);

    var existingPlayers = await playerTableService.Get(p =>
      p.Name.CompareTo(playerNameInput) >= 0 &&
      p.Name.CompareTo(upperBound) < 0);

    existingPlayers = existingPlayers.OrderBy(p => p.Name).ToList();

    var results = existingPlayers.Select(p => new AutocompleteResult() { Name = p.DisplayName, Value = p.DisplayName }).ToArray();

    return AutocompletionResult.FromSuccess(results.Take(25));
  }

  private static string NextPrefix(string prefix)
  {
    char lastChar = prefix[prefix.Length - 1];
    char nextChar = (char)(lastChar + 1);
    return prefix.Substring(0, prefix.Length - 1) + nextChar;
  }
}
