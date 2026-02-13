using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceCore.Models
{
    public class Contract
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Number { get; set; }

        [Required]
        public int ContractTypeId { get; set; }
        public ContractType? ContractType { get; set; }

        [Required]
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RenewalDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Pending Approval, Approved, Active, Expiring Soon, Expired, Terminated

        public string? SLADetails { get; set; }
        public string? RenewalTerms { get; set; }
        public string? PaymentTerms { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public int? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ContractAttachment> Attachments { get; set; } = new List<ContractAttachment>();
        public ICollection<ContractApproval> Approvals { get; set; } = new List<ContractApproval>();
        public ICollection<ContractPayment> Payments { get; set; } = new List<ContractPayment>();
        public ICollection<ContractHistory> History { get; set; } = new List<ContractHistory>();
    }
}
