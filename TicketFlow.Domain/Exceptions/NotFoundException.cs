namespace TicketFlow.Domain.Exceptions
{
    public class NotFoundException(string message) : DomainException(message);
}