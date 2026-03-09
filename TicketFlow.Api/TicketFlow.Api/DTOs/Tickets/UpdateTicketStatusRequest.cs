using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Api.DTOs.Tickets;

public class UpdateTicketStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Open|InProgress|Closed)$", ErrorMessage = "Status must be Open, InProgress, or Closed.")]
    public string Status { get; set; } = string.Empty;
}
