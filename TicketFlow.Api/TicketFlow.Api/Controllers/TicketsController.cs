using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Api.DTOs;
using TicketFlow.Api.DTOs.Tickets;
using TicketFlow.Api.Services.Interfaces;

namespace TicketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Create a new support ticket.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var userId = GetUserId();
        var ticketId = await _ticketService.CreateTicketAsync(request, userId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<object>.SuccessResponse(new { TicketId = ticketId }, "Ticket created successfully."));
    }

    /// <summary>
    /// Get list of tickets. Users see their own tickets; admins see all.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TicketListResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTickets()
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var tickets = await _ticketService.GetTicketsAsync(userId, role);
        return Ok(ApiResponse<IEnumerable<TicketListResponse>>.SuccessResponse(tickets));
    }

    /// <summary>
    /// Get list of admin users for ticket assignment.
    /// </summary>
    [HttpGet("admins")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AdminUserResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminUsers()
    {
        var admins = await _ticketService.GetAdminUsersAsync();
        return Ok(ApiResponse<IEnumerable<AdminUserResponse>>.SuccessResponse(admins));
    }

    /// <summary>
    /// Get detailed information about a specific ticket.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTicketDetail(int id)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var detail = await _ticketService.GetTicketDetailAsync(id, userId, role);
        return Ok(ApiResponse<TicketDetailResponse>.SuccessResponse(detail));
    }

    /// <summary>
    /// Assign a ticket to an admin user. (Admin only)
    /// </summary>
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketRequest request)
    {
        await _ticketService.AssignTicketAsync(id, request);
        return Ok(ApiResponse.SuccessResponse("Ticket assigned successfully."));
    }

    /// <summary>
    /// Update the status of a ticket. (Admin only)
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTicketStatusRequest request)
    {
        var adminUserId = GetUserId();
        await _ticketService.UpdateStatusAsync(id, request, adminUserId);
        return Ok(ApiResponse.SuccessResponse("Ticket status updated successfully."));
    }

    /// <summary>
    /// Add a comment to a ticket.
    /// </summary>
    [HttpPost("{id:int}/comments")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        await _ticketService.AddCommentAsync(id, request, userId, role);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse.SuccessResponse("Comment added successfully."));
    }

    /// <summary>
    /// Get status change history for a ticket.
    /// </summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TicketHistoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTicketHistory(int id)
    {
        var history = await _ticketService.GetTicketHistoryAsync(id);
        return Ok(ApiResponse<IEnumerable<TicketHistoryResponse>>.SuccessResponse(history));
    }

    #region Helper Methods

    private int GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return int.Parse(userIdClaim);
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("User role not found in token.");
    }

    #endregion
}
