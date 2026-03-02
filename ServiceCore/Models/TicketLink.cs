using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceCore.Models
{
    public class TicketLink
    {
        public int Id { get; set; }

        public int SourceTicketId { get; set; }
        [ForeignKey("SourceTicketId")]
        public Ticket? SourceTicket { get; set; }

        public int TargetTicketId { get; set; }
        [ForeignKey("TargetTicketId")]
        public Ticket? TargetTicket { get; set; }

        [Required]
        [StringLength(50)]
        public string LinkType { get; set; } = "Related"; // Duplicates, Blocks, Relates to, etc.

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
