namespace TicketFlow.Domain.Exceptions
{
    public class BookingLimitExceededException(string message) : DomainException(message);
}