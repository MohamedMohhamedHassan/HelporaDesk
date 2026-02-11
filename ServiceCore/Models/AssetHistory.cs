using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class AssetHistory
    {
        public int Id { get; set; }

        public int AssetId { get; set; }
        public Asset? Asset { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // Registration, Assignment, Maintenance, Update, Disposal

        [StringLength(100)]
        public string? ChangedBy { get; set; }

        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public string? StatusFrom { get; set; }
        public string? StatusTo { get; set; }
    }
}
