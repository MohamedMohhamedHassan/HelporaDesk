using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class TicketCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        // Hierarchical structure
        public int? ParentId { get; set; }
        public TicketCategory? Parent { get; set; }
        public ICollection<TicketCategory> Children { get; set; } = new List<TicketCategory>();
    }
}
