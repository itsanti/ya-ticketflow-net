namespace TicketFlow.Bookings.Domain.Exceptions
{
    public class BookingLimitExceededException(string message) : DomainException(message);
}
