using Dapper;
using TicketFlow.Api.Data;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;

namespace TicketFlow.Api.Repositories;

public class TicketCommentRepository : ITicketCommentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TicketCommentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(TicketComment comment)
    {
        const string sql = @"
            INSERT INTO TicketComments (TicketId, UserId, Comment, IsInternal, CreatedAt)
            VALUES (@TicketId, @UserId, @Comment, @IsInternal, @CreatedAt)";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, comment);
    }

    public async Task<IEnumerable<TicketComment>> GetByTicketIdAsync(int ticketId)
    {
        const string sql = "SELECT * FROM TicketComments WHERE TicketId = @TicketId ORDER BY CreatedAt ASC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<TicketComment>(sql, new { TicketId = ticketId });
    }
}
