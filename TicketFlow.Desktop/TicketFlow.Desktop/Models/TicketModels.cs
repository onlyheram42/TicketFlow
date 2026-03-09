using System;
using System.Collections.Generic;

namespace TicketFlow.Desktop.Models
{
    public class TicketListResponse
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AssignedToName { get; set; }
    }

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
        public List<TicketCommentResponse> Comments { get; set; } = new();
        public List<TicketHistoryResponse> History { get; set; } = new();
    }

    public class TicketCommentResponse
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TicketHistoryResponse
    {
        public int Id { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string ChangedByName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }

    public class CreateTicketRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
    }

    public class UpdateTicketStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class AssignTicketRequest
    {
        public int AssignToUserId { get; set; }
    }

    public class AddCommentRequest
    {
        public string Comment { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }

    public class AdminUserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
