using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class ContractType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
