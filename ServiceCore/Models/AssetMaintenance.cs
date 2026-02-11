using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class AssetMaintenance
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        [Required]
        public DateTime MaintenanceDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string Type { get; set; } = "Routine"; // Routine, Repair, Upgrade, Inspection

        public string? Description { get; set; }

        [DataType(DataType.Currency)]
        public decimal? Cost { get; set; }

        [StringLength(100)]
        public string? PerformedBy { get; set; }

        public DateTime? NextServiceDate { get; set; }
    }
}
