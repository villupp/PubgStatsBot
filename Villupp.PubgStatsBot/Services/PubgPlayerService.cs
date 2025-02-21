using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.Api.Pubg;
using Villupp.PubgStatsBot.TableStorage;
using Villupp.PubgStatsBot.TableStorage.Models;

namespace Villupp.PubgStatsBot.Services;

public class PubgPlayerService(
  ILogger<PubgPlayerService> logger,
  TableStorageService<PubgPlayer> playerTableService,
  PubgApiClient pubgClient
  )
{
  private readonly ILogger<PubgPlayerService> logger = logger;
  private readonly PubgApiClient pubgClient = pubgClient;

  public async Task<PubgPlayer> GetOrCreatePubgPlayer(string playerName)
  {
    var players = await playerTableService.Get(p => p.Name == playerName.ToLower());

    if (players != null && players.Count > 0)
    {
      players = [.. players.OrderByDescending(p => p.Timestamp)];
      return players.First();
    }

    logger.LogInformation($"Player not found in cache by name '{playerName}'. Requesting from API..");

    // If not found --> retrieve from API
    var player = await pubgClient.GetPlayer(playerName);

    if (player?.Attributes == null)
      return null;

    var pubgPlayer = new PubgPlayer()
    {
      Id = player.Id,
      Name = player.Attributes.Name.ToLower(),
      DisplayName = player.Attributes.Name
    };

    // Get existing players with same ID from cache and update if found
    var existingPlayers = await playerTableService.Get(p =>
        p.Id == player.Id.ToLower());
    existingPlayers = [.. existingPlayers.OrderByDescending(p => p.Timestamp)];

    if (existingPlayers.Count > 0)
    {
      var newestExistingPlayer = existingPlayers.FirstOrDefault();

      if (existingPlayers.Count > 1)

        await DeletePlayers(existingPlayers.Skip(1));

      newestExistingPlayer.DisplayName = playerName;
      newestExistingPlayer.Name = playerName.ToLower();

      await playerTableService.Update(newestExistingPlayer);
      logger.LogInformation($"Updated player '{pubgPlayer.DisplayName}' ID {pubgPlayer.Id} name in storage cache.");
    }
    else
    {
      await playerTableService.Add(pubgPlayer);
      logger.LogInformation($"Added player '{pubgPlayer.DisplayName}' ID {pubgPlayer.Id} to storage cache.");
    }
    
    return pubgPlayer;
  }

  private async Task DeletePlayers(IEnumerable<PubgPlayer> players)
  {
    foreach (var player in players)
      await DeletePlayer(player);
  }

  private async Task DeletePlayer(PubgPlayer player)
  {
    await playerTableService.Delete(player);
    logger.LogInformation($"Deleted duplicate player {player.DisplayName} ID {player.Id}");
  }
}