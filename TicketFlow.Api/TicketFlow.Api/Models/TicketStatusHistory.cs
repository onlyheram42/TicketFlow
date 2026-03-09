namespace TicketFlow.Api.Models;

public class TicketStatusHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
}
