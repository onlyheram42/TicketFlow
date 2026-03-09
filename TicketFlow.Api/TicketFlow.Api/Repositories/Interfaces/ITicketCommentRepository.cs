using TicketFlow.Api.Models;

namespace TicketFlow.Api.Repositories.Interfaces;

public interface ITicketCommentRepository
{
    Task AddAsync(TicketComment comment);
    Task<IEnumerable<TicketComment>> GetByTicketIdAsync(int ticketId);
}
