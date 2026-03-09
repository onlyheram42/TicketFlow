using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Api.DTOs.Tickets;

public class AddCommentRequest
{
    [Required(ErrorMessage = "Comment is required.")]
    public string Comment { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false;
}
