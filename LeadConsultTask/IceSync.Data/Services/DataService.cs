using Microsoft.EntityFrameworkCore;

namespace IceSync.Data.Services
{
    public abstract class DataService<TEntity> : IDataService<TEntity>
        where TEntity : class
    {
        public DbContext Data { get; }

        public DataService(DbContext data)
        {
            Data = data;
        }

        public IQueryable<TEntity> All() => Data.Set<TEntity>();

        public async Task Add(TEntity entity)
        {
            await Data.AddAsync(entity);
            await Data.SaveChangesAsync();
        }

        public async Task AddRange(IEnumerable<TEntity> entities)
        {
            await Data.AddRangeAsync(entities);
            await Data.SaveChangesAsync();
        }

        public async Task Remove(TEntity entity)
        {
            Data.Remove(entity);
            await Data.SaveChangesAsync();
        }

        public async Task RemoveRange(IEnumerable<TEntity> entities)
        {
            Data.RemoveRange(entities);
            await Data.SaveChangesAsync();
        }
    }
}