using TicketFlow.Users.Domain.Enums;

namespace TicketFlow.Users.Application.Abstractions
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Guid userId, string login, UserRole role);
    }
}
