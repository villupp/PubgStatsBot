using System.Linq.Expressions;

namespace Villupp.PubgStatsBot.TableStorage
{
    internal interface ITableStorageService<T>
    {
        public Task<List<T>> Get(Expression<Func<T, bool>> query);

        public Task<List<T>> Get();

        public Task<bool> Add(T entity);

        public Task<bool> Delete(T entity);

        public Task<bool> Update(T entity);
    }
}