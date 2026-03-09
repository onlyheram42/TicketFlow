using TicketFlow.Api.DTOs.Auth;
using TicketFlow.Api.DTOs.Tickets;

namespace TicketFlow.Api.Services.Interfaces;

public interface ITicketService
{
    Task<int> CreateTicketAsync(CreateTicketRequest request, int userId);
    Task<IEnumerable<TicketListResponse>> GetTicketsAsync(int userId, string role);
    Task<TicketDetailResponse> GetTicketDetailAsync(int ticketId, int userId, string role);
    Task AssignTicketAsync(int ticketId, AssignTicketRequest request);
    Task UpdateStatusAsync(int ticketId, UpdateTicketStatusRequest request, int adminUserId);
    Task AddCommentAsync(int ticketId, AddCommentRequest request, int userId, string role);
    Task<IEnumerable<TicketHistoryResponse>> GetTicketHistoryAsync(int ticketId);
    Task<IEnumerable<AdminUserResponse>> GetAdminUsersAsync();
}
