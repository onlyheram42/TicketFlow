namespace TicketFlow.Api.DTOs.Tickets;

public class TicketHistoryResponse
{
    public int Id { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
