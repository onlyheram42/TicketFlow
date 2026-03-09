using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Api.DTOs.Tickets;

public class AssignTicketRequest
{
    [Required(ErrorMessage = "AssignToUserId is required.")]
    public int AssignToUserId { get; set; }
}
