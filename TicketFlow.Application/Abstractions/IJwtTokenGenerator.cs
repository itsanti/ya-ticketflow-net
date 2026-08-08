using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Abstractions
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Guid userId, string login, UserRole role);
    }
}
