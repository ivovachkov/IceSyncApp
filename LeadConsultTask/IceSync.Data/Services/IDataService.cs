namespace IceSync.Data.Services
{
    public interface IDataService<TEntity> where TEntity : class
    {
        IQueryable<TEntity> All();

        Task Add(TEntity entity);
    }
}