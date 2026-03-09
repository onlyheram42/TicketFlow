using Dapper;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;

namespace TicketFlow.Api.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TicketRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(Ticket ticket)
    {
        const string sql = @"
            INSERT INTO Tickets (TicketNumber, CreatedByUserId, Subject, Description, Priority, Status, CreatedAt, UpdatedAt)
            VALUES (@TicketNumber, @CreatedByUserId, @Subject, @Description, @Priority, @Status, @CreatedAt, @UpdatedAt);
            SELECT LAST_INSERT_ID();";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, ticket);
    }

    public async Task<Ticket?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Tickets WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Ticket>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Ticket>> GetByUserIdAsync(int userId)
    {
        const string sql = "SELECT * FROM Tickets WHERE CreatedByUserId = @UserId ORDER BY CreatedAt DESC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Ticket>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync()
    {
        const string sql = "SELECT * FROM Tickets ORDER BY CreatedAt DESC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Ticket>(sql);
    }

    public async Task UpdateAssignmentAsync(int ticketId, int adminUserId)
    {
        const string sql = "UPDATE Tickets SET AssignedToUserId = @AdminUserId, UpdatedAt = UTC_TIMESTAMP() WHERE Id = @TicketId";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { TicketId = ticketId, AdminUserId = adminUserId });
    }

    public async Task UpdateStatusAsync(int ticketId, string status)
    {
        const string sql = "UPDATE Tickets SET Status = @Status, UpdatedAt = UTC_TIMESTAMP() WHERE Id = @TicketId";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { TicketId = ticketId, Status = status });
    }

    public async Task<string> GetNextTicketNumberAsync()
    {
        const string sql = "SELECT MAX(Id) FROM Tickets";

        using var connection = _connectionFactory.CreateConnection();
        var maxId = await connection.ExecuteScalarAsync<int?>(sql);
        var nextNumber = (maxId ?? 0) + 1;
        return $"TKT-{nextNumber:D5}";
    }
}
