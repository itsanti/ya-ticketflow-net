
namespace TicketFlow.Models.Store
{
    public class InMemoryEventStore : IInMemoryStore<Event>
    {
        private readonly List<Event> _events = [];
        private readonly Lock _lock = new();

        public Task<IEnumerable<Event>> GetAllAsync()
        {
            lock (_lock)
            {
                return Task.FromResult<IEnumerable<Event>>([.. _events]);
            }
        }

        public Task AddAsync(Event entity)
        {
            lock (_lock)
            {
                _events.Add(entity);
            }
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            lock (_lock)
            {
                var item = _events.FirstOrDefault(e => e.Id == id);
                if (item != null)
                {
                    return Task.FromResult(_events.Remove(item));
                }
                return Task.FromResult(false);
            }
        }

        public Task<IEnumerable<Event>> FindAsync(Func<Event, bool> predicate)
        {
            lock (_lock)
            {
                var results = _events.Where(predicate).ToList();
                return Task.FromResult<IEnumerable<Event>>(results);
            }
        }

        public Task<Event> UpdateAsync(Event entity)
        {
            lock (_lock)
            {
                var index = _events.FindIndex(e => e.Id == entity.Id);
                if (index == -1)
                {
                    throw new Exceptions.NotFoundException($"Event with ID {entity.Id} not found in store.");
                }
                _events[index] = entity;
                return Task.FromResult(entity);
            }
        }
    }
}
