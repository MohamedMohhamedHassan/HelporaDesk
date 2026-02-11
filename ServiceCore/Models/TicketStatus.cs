using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class TicketStatus
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
