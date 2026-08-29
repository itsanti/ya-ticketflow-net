namespace TicketFlow.Bookings.Domain.Exceptions
{
    public class NotFoundException(string message) : DomainException(message);
}
