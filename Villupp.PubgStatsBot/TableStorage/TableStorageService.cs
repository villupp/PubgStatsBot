using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Villupp.PubgStatsBot.TableStorage
{
    /* T should be a model class for the table with exact same name as table name in Azure Storage */

    public class TableStorageService<T> : ITableStorageService<T> where T : class, ITableEntity, new()
    {
        private readonly ILogger logger;
        private readonly TableClient tableClient;

        public TableStorageService(ILogger<TableStorageService<T>> logger, TableServiceClient tableServiceClient)
        {
            this.logger = logger;
            tableClient = tableServiceClient.GetTableClient(typeof(T).Name);
            // Create TableStorage tables if they do not exist
            tableServiceClient.CreateTableIfNotExists("trackedDotaPlayer");
            tableServiceClient.CreateTableIfNotExists("command");
            tableServiceClient.CreateTableIfNotExists("pubgLeaderboardPlayer");
        }

        public async Task<List<T>> Get(Expression<Func<T, bool>> query)
        {
            try
            {
                return await tableClient.QueryAsync(query).ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Get from table {typeof(T).Name} failed: {ex}");
                return new List<T>();
            }
        }

        public async Task<List<T>> Get()
        {
            try
            {
                return await tableClient.QueryAsync<T>().ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Get from table {typeof(T).Name} failed: {ex}");
                return new List<T>();
            }
        }

        public async Task<bool> Add(T entity)
        {
            logger.LogInformation($"Add {typeof(T).Name} entity RowKey: {entity.RowKey}");

            try
            {
                var response = await tableClient.AddEntityAsync(entity);
                return !response.IsError;
            }
            catch (Exception ex)
            {
                logger.LogError($"Add to table {typeof(T).Name} failed for RowKey: {entity.RowKey}: {ex}");
                return false;
            }
        }

        public async Task<bool> Delete(T entity)
        {
            logger.LogInformation($"Delete {typeof(T).Name} entity RowKey: {entity.RowKey}");

            try
            {
                var response = await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                return !response.IsError;
            }
            catch (Exception ex)
            {
                logger.LogError($"Delete from table {typeof(T).Name} failed RowKey: {entity.RowKey}: {ex}");
                return false;
            }
        }

        public async Task<bool> DeleteAll()
        {
            logger.LogInformation($"Delete all {typeof(T).Name}");

            try
            {
                var isSuccess = true;
                var allEntities = await Get();

                foreach (var entity in allEntities)
                {
                    try
                    {
                        await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Delete from table {typeof(T).Name} failed RowKey: {entity.RowKey}: {ex}");
                        isSuccess = false;
                    }
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                logger.LogError($"DeleteAll from table {typeof(T).Name} failed: {ex}");
                return false;
            }
        }

        public async Task<bool> Update(T entity)
        {
            logger.LogInformation($"Update {typeof(T).Name} entity RowKey: {entity.RowKey}");

            try
            {
                var response = await tableClient.UpdateEntityAsync(entity, entity.ETag);
                return !response.IsError;
            }
            catch (Exception ex)
            {
                logger.LogError($"Update table {typeof(T).Name} failed RowKey: {entity.RowKey}: {ex}");
                return false;
            }
        }
    }
}