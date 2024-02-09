using Azure.Data.Tables;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Villupp.PubgStatsBot.TableStorage.Repositories
{
    public class CachedRepository<T> where T : class, ITableEntity, new()
    {
        private const string ALL_SEASONS_CACHE_KEY_PREFIX = "ALL_SEASONS_CACHE_KEY";

        private readonly ILogger logger;
        private readonly TableStorageService<T> tableService;
        private readonly IMemoryCache memoryCache;

        private static readonly SemaphoreSlim lockSemaphore = new(1);

        private string cacheKey = null;

        protected int CacheLifetimeMinutes = 1;

        public CachedRepository(
            ILogger<CachedRepository<T>> logger,
            TableStorageService<T> tableService,
            IMemoryCache memoryCache
            )
        {
            this.logger = logger;
            this.tableService = tableService;
            this.memoryCache = memoryCache;

            cacheKey = $"{ALL_SEASONS_CACHE_KEY_PREFIX}-{GetType()}";
        }

        public async Task<List<T>> Get(Expression<Func<T, bool>> query)
        {
            var commands = await tableService.Get(query);

            return commands;
        }

        public async Task<List<T>> Get(bool useCache = true)
        {
            if (!useCache) return await tableService.Get();

            if (memoryCache.TryGetValue(cacheKey, out List<T> cachedRecords))
            {
                logger.LogDebug($"Got {cachedRecords.Count} objects from cache {GetType()}");
                return cachedRecords;
            }
            else
            {
                logger.LogDebug($"{GetType()} records not in cache. Updating.");
                var allRecords = await tableService.Get();
                await UpdateCache(allRecords);
                return allRecords;
            }
        }

        public void FlushCache()
        {
            memoryCache.Remove(cacheKey);
        }

        private async Task UpdateCache(List<T> allRecords)
        {
            logger.LogDebug($"{GetType()} updated cache with {allRecords.Count} items");

            await lockSemaphore.WaitAsync();

            try
            {
                memoryCache.Set(cacheKey, allRecords, DateTimeOffset.Now + TimeSpan.FromMinutes(CacheLifetimeMinutes));
            }
            catch (Exception ex)
            {
                logger.LogWarning($"CommandRepository.UpdateCache failed: {ex}");
            }
            finally
            {
                lockSemaphore.Release();
            }
        }
    }
}