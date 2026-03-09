namespace TicketFlow.Api.DTOs.Tickets;

public class TicketDetailResponse
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public List<TicketCommentResponse> Comments { get; set; } = [];
    public List<TicketHistoryResponse> History { get; set; } = [];
}
