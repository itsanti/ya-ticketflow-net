namespace TicketFlow.Events.Domain.Exceptions
{
    public class NotFoundException(string message) : DomainException(message);
}
