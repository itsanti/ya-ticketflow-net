namespace TicketFlow.Models.Store
{
    public interface IInMemoryStore<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);
        Task<T> UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid id);
    }
}
