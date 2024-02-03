﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Villupp.PubgStatsBot.TableStorage.Models;

namespace Villupp.PubgStatsBot.TableStorage.Repositories
{
    public class PubgSeasonRepository : CachedRepository<PubgSeason>
    {
        private const string SEASONID_PREFIX_RANKED_SQUAD_FPP_PC = "division.bro.official.pc-2018-";

        public PubgSeasonRepository(
            ILogger<PubgSeasonRepository> logger,
            TableStorageService<PubgSeason> pubgSeasonTableService,
            IMemoryCache memoryCache
            ) : base(logger, pubgSeasonTableService, memoryCache)
        {
            CacheLifetimeMinutes = 720; // 12 hrs
        }

        public async Task<PubgSeason> GetCurrentSeason()
        {
            var seasons = await Get();

            return seasons.Where(s => s.IsCurrentSeason && s.Id.StartsWith(SEASONID_PREFIX_RANKED_SQUAD_FPP_PC)).FirstOrDefault();
        }
    }
}