using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class ContractApproval
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public Contract? Contract { get; set; }

        public int ApproverId { get; set; }
        public User? Approver { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string? Comments { get; set; }

        public DateTime? DecisionDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
