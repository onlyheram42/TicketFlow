using TicketFlow.Api.Models;

namespace TicketFlow.Api.Repositories.Interfaces;

public interface ITicketStatusHistoryRepository
{
    Task AddAsync(TicketStatusHistory history);
    Task<IEnumerable<TicketStatusHistory>> GetByTicketIdAsync(int ticketId);
}
