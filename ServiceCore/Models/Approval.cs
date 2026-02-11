using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class Approval
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? RequestType { get; set; } // e.g., "Ticket_Resolution", "Asset_Assignment", "Access_Request"

        public int? RelatedId { get; set; } // ID of the ticket, asset, etc.

        [Required]
        [StringLength(200)]
        public string? Subject { get; set; }

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        public int RequesterId { get; set; }
        public User? Requester { get; set; }

        public int? ApproverId { get; set; }
        public User? Approver { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ActedAt { get; set; }

        public string? Comments { get; set; }
    }
}
