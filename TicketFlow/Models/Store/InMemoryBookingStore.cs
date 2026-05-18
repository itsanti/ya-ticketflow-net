using TicketFlow.Exceptions;

namespace TicketFlow.Models.Store
{
    public class InMemoryBookingStore : IInMemoryStore<Booking>
    {
        private readonly List<Booking> _bookings = [];
        private readonly Lock _lock = new();

        public Task<IEnumerable<Booking>> GetAllAsync()
        {
            lock (_lock)
            {
                return Task.FromResult<IEnumerable<Booking>>([.. _bookings]);
            }
        }

        public Task AddAsync(Booking entity)
        {
            lock (_lock)
            {
                _bookings.Add(entity);
            }

            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            lock (_lock)
            {
                var item = _bookings.FirstOrDefault(e => e.Id == id);
                if (item != null)
                {
                    return Task.FromResult(_bookings.Remove(item));
                }
                return Task.FromResult(false);
            }
        }

        public Task<Booking> UpdateAsync(Booking entity)
        {
            lock (_lock)
            {
                var index = _bookings.FindIndex(e => e.Id == entity.Id);

                if (index == -1)
                {
                    throw new NotFoundException($"Booking with ID {entity.Id} not found in store.");
                }

                _bookings[index] = entity;
                return Task.FromResult(entity);
            }
        }

        public Task<IEnumerable<Booking>> FindAsync(Func<Booking, bool> predicate)
        {
            lock (_lock)
            {
                var results = _bookings.Where(predicate).ToList();
                return Task.FromResult<IEnumerable<Booking>>(results);
            }
        }
    }
}
