using TicketFlow.Exceptions;

namespace TicketFlow.Models
{
    public class Event
    {
        public Guid Id { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public required DateTime StartAt { get; set; }

        public required DateTime EndAt { get; set; }

        public required int TotalSeats { get; set; }

        public int AvailableSeats { get; private set; }

        public ICollection<Booking> Bookings { get; private set; } = [];

        private Event()
        {
            Title = null!;
        }

        public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
        {
            if (totalSeats <= 0)
                throw new ValidationException("TotalSeats must be greater than 0");

            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }

        public bool TryReserveSeats(int count = 1)
        {
            if (AvailableSeats < count)
            {
                return false;
            }

            AvailableSeats -= count;
            return true;
        }

        public void ReleaseSeats(int count = 1)
        {
            AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
        }
    }
}
