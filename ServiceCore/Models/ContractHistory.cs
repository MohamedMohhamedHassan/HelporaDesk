using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class ContractHistory
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public Contract? Contract { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public int? ChangedById { get; set; }
        public User? ChangedBy { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
