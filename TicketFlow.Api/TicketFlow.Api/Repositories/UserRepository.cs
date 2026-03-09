using Dapper;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;

namespace TicketFlow.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAdminUsersAsync()
    {
        const string sql = "SELECT * FROM Users WHERE Role = 'Admin'";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<User>(sql);
    }
}
