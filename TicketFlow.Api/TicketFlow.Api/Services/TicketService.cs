using TicketFlow.Api.DTOs.Tickets;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;
using TicketFlow.Api.Services.Interfaces;

namespace TicketFlow.Api.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketCommentRepository _commentRepository;
    private readonly ITicketStatusHistoryRepository _historyRepository;
    private readonly IUserRepository _userRepository;

    // Valid status transitions: Open → InProgress → Closed
    private static readonly Dictionary<string, string> ValidStatusTransitions = new()
    {
        { "Open", "InProgress" },
        { "InProgress", "Closed" }
    };

    public TicketService(
        ITicketRepository ticketRepository,
        ITicketCommentRepository commentRepository,
        ITicketStatusHistoryRepository historyRepository,
        IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _commentRepository = commentRepository;
        _historyRepository = historyRepository;
        _userRepository = userRepository;
    }

    public async Task<int> CreateTicketAsync(CreateTicketRequest request, int userId)
    {
        var ticketNumber = await _ticketRepository.GetNextTicketNumberAsync();
        var now = DateTime.UtcNow;

        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            CreatedByUserId = userId,
            Subject = request.Subject,
            Description = request.Description,
            Priority = request.Priority,
            Status = "Open",
            CreatedAt = now,
            UpdatedAt = now
        };

        var ticketId = await _ticketRepository.CreateAsync(ticket);

        // Log initial status
        await _historyRepository.AddAsync(new TicketStatusHistory
        {
            TicketId = ticketId,
            OldStatus = "",
            NewStatus = "Open",
            ChangedByUserId = userId,
            ChangedAt = now
        });

        return ticketId;
    }

    public async Task<IEnumerable<TicketListResponse>> GetTicketsAsync(int userId, string role)
    {
        var tickets = role == "Admin"
            ? await _ticketRepository.GetAllAsync()
            : await _ticketRepository.GetByUserIdAsync(userId);

        var result = new List<TicketListResponse>();

        foreach (var ticket in tickets)
        {
            string? assignedToName = null;
            if (ticket.AssignedToUserId.HasValue)
            {
                var assignedUser = await _userRepository.GetByIdAsync(ticket.AssignedToUserId.Value);
                assignedToName = assignedUser?.FullName;
            }

            result.Add(new TicketListResponse
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Subject = ticket.Subject,
                Priority = ticket.Priority,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                AssignedToName = assignedToName
            });
        }

        return result;
    }

    public async Task<TicketDetailResponse> GetTicketDetailAsync(int ticketId, int userId, string role)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");

        // Users can only view their own tickets
        if (role != "Admin" && ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this ticket.");
        }

        var createdByUser = await _userRepository.GetByIdAsync(ticket.CreatedByUserId);
        User? assignedUser = null;
        if (ticket.AssignedToUserId.HasValue)
        {
            assignedUser = await _userRepository.GetByIdAsync(ticket.AssignedToUserId.Value);
        }

        // Get comments — filter internal comments for non-admin users
        var comments = await _commentRepository.GetByTicketIdAsync(ticketId);
        var commentResponses = new List<TicketCommentResponse>();
        foreach (var comment in comments)
        {
            if (comment.IsInternal && role != "Admin")
                continue;

            var commentUser = await _userRepository.GetByIdAsync(comment.UserId);
            commentResponses.Add(new TicketCommentResponse
            {
                Id = comment.Id,
                Comment = comment.Comment,
                IsInternal = comment.IsInternal,
                UserName = commentUser?.FullName ?? "Unknown",
                CreatedAt = comment.CreatedAt
            });
        }

        // Get history
        var historyEntries = await _historyRepository.GetByTicketIdAsync(ticketId);
        var historyResponses = new List<TicketHistoryResponse>();
        foreach (var entry in historyEntries)
        {
            var changedByUser = await _userRepository.GetByIdAsync(entry.ChangedByUserId);
            historyResponses.Add(new TicketHistoryResponse
            {
                Id = entry.Id,
                OldStatus = entry.OldStatus,
                NewStatus = entry.NewStatus,
                ChangedByName = changedByUser?.FullName ?? "Unknown",
                ChangedAt = entry.ChangedAt
            });
        }

        return new TicketDetailResponse
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Subject = ticket.Subject,
            Description = ticket.Description,
            Priority = ticket.Priority,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            CreatedByName = createdByUser?.FullName ?? "Unknown",
            AssignedToName = assignedUser?.FullName,
            Comments = commentResponses,
            History = historyResponses
        };
    }

    public async Task AssignTicketAsync(int ticketId, AssignTicketRequest request)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");

        if (ticket.Status == "Closed")
        {
            throw new InvalidOperationException("Cannot assign a closed ticket.");
        }

        var adminUser = await _userRepository.GetByIdAsync(request.AssignToUserId)
            ?? throw new KeyNotFoundException($"User with ID {request.AssignToUserId} not found.");

        if (adminUser.Role != "Admin")
        {
            throw new InvalidOperationException("Tickets can only be assigned to admin users.");
        }

        await _ticketRepository.UpdateAssignmentAsync(ticketId, request.AssignToUserId);
    }

    public async Task UpdateStatusAsync(int ticketId, UpdateTicketStatusRequest request, int adminUserId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");

        if (ticket.Status == "Closed")
        {
            throw new InvalidOperationException("Cannot modify a closed ticket.");
        }

        // Validate status transition
        if (!ValidStatusTransitions.TryGetValue(ticket.Status, out var allowedNextStatus)
            || allowedNextStatus != request.Status)
        {
            throw new InvalidOperationException(
                $"Invalid status transition from '{ticket.Status}' to '{request.Status}'. " +
                $"Allowed: {ticket.Status} → {allowedNextStatus ?? "none"}.");
        }

        await _ticketRepository.UpdateStatusAsync(ticketId, request.Status);

        // Log status change
        await _historyRepository.AddAsync(new TicketStatusHistory
        {
            TicketId = ticketId,
            OldStatus = ticket.Status,
            NewStatus = request.Status,
            ChangedByUserId = adminUserId,
            ChangedAt = DateTime.UtcNow
        });
    }

    public async Task AddCommentAsync(int ticketId, AddCommentRequest request, int userId, string role)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");

        if (ticket.Status == "Closed")
        {
            throw new InvalidOperationException("Cannot add comments to a closed ticket.");
        }

        // Users can only comment on their own tickets
        if (role != "Admin" && ticket.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("You can only add comments to your own tickets.");
        }

        // Only admins can add internal comments
        if (request.IsInternal && role != "Admin")
        {
            throw new UnauthorizedAccessException("Only admins can add internal comments.");
        }

        await _commentRepository.AddAsync(new TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            Comment = request.Comment,
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<IEnumerable<TicketHistoryResponse>> GetTicketHistoryAsync(int ticketId)
    {
        var historyEntries = await _historyRepository.GetByTicketIdAsync(ticketId);
        var responses = new List<TicketHistoryResponse>();

        foreach (var entry in historyEntries)
        {
            var user = await _userRepository.GetByIdAsync(entry.ChangedByUserId);
            responses.Add(new TicketHistoryResponse
            {
                Id = entry.Id,
                OldStatus = entry.OldStatus,
                NewStatus = entry.NewStatus,
                ChangedByName = user?.FullName ?? "Unknown",
                ChangedAt = entry.ChangedAt
            });
        }

        return responses;
    }

    public async Task<IEnumerable<AdminUserResponse>> GetAdminUsersAsync()
    {
        var admins = await _userRepository.GetAdminUsersAsync();
        return admins.Select(a => new AdminUserResponse
        {
            Id = a.Id,
            FullName = a.FullName
        });
    }
}
