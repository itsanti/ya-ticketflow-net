using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.Services
{
    public interface IUserService
    {
        Task RegisterAsync(string login, string password, UserRole role);
        Task<string> LoginAsync(string login, string password);
    }
}
