namespace TicketFlow.Api.DTOs.Tickets;

public class TicketCommentResponse
{
    public int Id { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
