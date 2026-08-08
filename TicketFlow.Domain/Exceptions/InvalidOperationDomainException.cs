namespace TicketFlow.Domain.Exceptions
{
    public class InvalidOperationDomainException(string message) : DomainException(message);
}
