using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class Asset
    {
        public int Id { get; set; }
        
        [StringLength(100)]
        public string? Tag { get; set; }

        [Required]
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? Vendor { get; set; }

        [StringLength(100)]
        public string? Status { get; set; } // Available, In Use, Under Maintenance, Retired, Disposed

        [StringLength(200)]
        public string? Location { get; set; }

        public int? CategoryId { get; set; }
        public AssetCategory? Category { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? WarrantyExpiry { get; set; }

        [DataType(DataType.Currency)]
        public decimal? PurchaseCost { get; set; }

        [DataType(DataType.Currency)]
        public decimal? ResidualValue { get; set; }

        public int? UsefulLifeMonths { get; set; }

        [StringLength(50)]
        public string? DepreciationMethod { get; set; } // Straight Line, Double Declining, etc.

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
        public ICollection<AssetMaintenance> Maintenances { get; set; } = new List<AssetMaintenance>();
        public ICollection<AssetHistory> Histories { get; set; } = new List<AssetHistory>();
    }
}
