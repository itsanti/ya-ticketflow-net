namespace TicketFlow.Domain.Exceptions
{
    public class ForbiddenException(string message) : DomainException(message);
}
