namespace TicketFlow.Users.Domain.Exceptions
{
    public class UnauthorizedException(string message) : DomainException(message);
}
