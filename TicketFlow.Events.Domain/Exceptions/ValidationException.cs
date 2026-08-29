namespace TicketFlow.Events.Domain.Exceptions
{
    public class ValidationException(string message) : DomainException(message);
}
