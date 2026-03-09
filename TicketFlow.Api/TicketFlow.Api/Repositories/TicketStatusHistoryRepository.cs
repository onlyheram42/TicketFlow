using Dapper;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;

namespace TicketFlow.Api.Repositories;

public class TicketStatusHistoryRepository : ITicketStatusHistoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TicketStatusHistoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(TicketStatusHistory history)
    {
        const string sql = @"
            INSERT INTO TicketStatusHistory (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt)
            VALUES (@TicketId, @OldStatus, @NewStatus, @ChangedByUserId, @ChangedAt)";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, history);
    }

    public async Task<IEnumerable<TicketStatusHistory>> GetByTicketIdAsync(int ticketId)
    {
        const string sql = "SELECT * FROM TicketStatusHistory WHERE TicketId = @TicketId ORDER BY ChangedAt ASC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<TicketStatusHistory>(sql, new { TicketId = ticketId });
    }
}
