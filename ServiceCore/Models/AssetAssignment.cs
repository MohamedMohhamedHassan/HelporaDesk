using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class AssetAssignment
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ReturnDate { get; set; }

        public string? ConditionOnAssignment { get; set; }
        public string? ConditionOnReturn { get; set; }
        
        public string? Notes { get; set; }
    }
}
