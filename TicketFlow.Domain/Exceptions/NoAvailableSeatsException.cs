namespace TicketFlow.Domain.Exceptions
{
    public class NoAvailableSeatsException(string message) : DomainException(message);
}
