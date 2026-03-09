using TicketFlow.Api.Models;

namespace TicketFlow.Api.Repositories.Interfaces;

public interface ITicketRepository
{
    Task<int> CreateAsync(Ticket ticket);
    Task<Ticket?> GetByIdAsync(int id);
    Task<IEnumerable<Ticket>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Ticket>> GetAllAsync();
    Task UpdateAssignmentAsync(int ticketId, int adminUserId);
    Task UpdateStatusAsync(int ticketId, string status);
    Task<string> GetNextTicketNumberAsync();
}
