using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceCore.Models
{
    public class ChangeRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Change Type: Standard, Normal, Emergency
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Normal";

        // Category: Hardware, Software, Network, etc.
        public int? CategoryId { get; set; }
        public TicketCategory? Category { get; set; }

        // Risk Level: Low, Medium, High, Critical
        public string RiskLevel { get; set; } = "Low";

        // Impact: Low, Medium, High, Extensive
        public string Impact { get; set; } = "Low";

        // Priority: Low, Medium, High, Critical
        public string Priority { get; set; } = "Medium";

        // Status: Draft, Submitted, Under Assessment, Approved, Scheduled, In Progress, Implemented, Closed, Rejected, Rolled Back
        public string Status { get; set; } = "Draft";

        public int RequestedById { get; set; }
        [ForeignKey("RequestedById")]
        public User? RequestedBy { get; set; }

        public int? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public User? AssignedTo { get; set; }

        public DateTime? PlannedStartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        public string? RollbackPlan { get; set; }
        public string? ImplementationPlan { get; set; }
        public string? TestPlan { get; set; }
        
        public string? ClosureNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // Navigation Properties
        public ICollection<ChangeApproval> Approvals { get; set; } = new List<ChangeApproval>();
        public ICollection<ChangeTask> Tasks { get; set; } = new List<ChangeTask>();
        public ICollection<ChangeActivity> Activities { get; set; } = new List<ChangeActivity>();
        public ICollection<ChangeAsset> LinkedAssets { get; set; } = new List<ChangeAsset>();
    }

    public class ChangeApproval
    {
        public int Id { get; set; }
        public int ChangeRequestId { get; set; }
        [ForeignKey("ChangeRequestId")]
        public ChangeRequest? ChangeRequest { get; set; }

        public int ApproverId { get; set; }
        [ForeignKey("ApproverId")]
        public User? Approver { get; set; }

        // Status: Pending, Approved, Rejected
        public string Status { get; set; } = "Pending";
        public string? Comments { get; set; }
        public DateTime? ActionedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ChangeTask
    {
        public int Id { get; set; }
        public int ChangeRequestId { get; set; }
        [ForeignKey("ChangeRequestId")]
        public ChangeRequest? ChangeRequest { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public int? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public User? AssignedTo { get; set; }

        // Status: Pending, In Progress, Completed, Failed
        public string Status { get; set; } = "Pending";
        
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ChangeActivity
    {
        public int Id { get; set; }
        public int ChangeRequestId { get; set; }
        [ForeignKey("ChangeRequestId")]
        public ChangeRequest? ChangeRequest { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ChangeAsset
    {
        public int Id { get; set; }
        public int ChangeRequestId { get; set; }
        [ForeignKey("ChangeRequestId")]
        public ChangeRequest? ChangeRequest { get; set; }

        public int AssetId { get; set; }
        [ForeignKey("AssetId")]
        public Asset? Asset { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.Now;
    }
}
