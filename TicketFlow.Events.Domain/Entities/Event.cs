using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Domain.Entities
{
    public class Event
    {
        public Guid Id { get; set; }

        public required string Title { get; set; }

        public string? Description { get; set; }

        public required DateTime StartAt { get; set; }

        public required DateTime EndAt { get; set; }

        public int TotalSeats { get; private set; }

        public int AvailableSeats { get; private set; }

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

        public void ChangeCapacity(int totalSeats)
        {
            if (totalSeats <= 0)
                throw new ValidationException("TotalSeats must be greater than 0");

            var reservedSeats = TotalSeats - AvailableSeats;

            if (totalSeats < reservedSeats)
                throw new ValidationException(
                    $"TotalSeats can not be less than {reservedSeats} already reserved seat(s).");

            TotalSeats = totalSeats;
            AvailableSeats = totalSeats - reservedSeats;
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
