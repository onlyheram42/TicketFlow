using TicketFlow.Api.Models;

namespace TicketFlow.Api.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAdminUsersAsync();
}
