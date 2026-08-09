namespace TicketFlow.Application.Services
{
    public interface IUserService
    {
        Task RegisterAsync(string login, string password);
        Task<string> LoginAsync(string login, string password);
    }
}
