using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string? Subject { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        // Foreign Keys & Navigation
        public int StatusId { get; set; }
        public TicketStatus? Status { get; set; }

        public int PriorityId { get; set; }
        public TicketPriority? Priority { get; set; }

        public int CategoryId { get; set; }
        public TicketCategory? Category { get; set; }

        public int RequesterId { get; set; }
        public User? Requester { get; set; }

        public int? AssignedId { get; set; }
        public User? Assigned { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ResolutionDate { get; set; }

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();

        public void CalculateDueDate(string priorityName)
        {
            int hours = priorityName.ToLower() switch
            {
                "critical" => 4,
                "high" => 8,
                "medium" => 24,
                "low" => 48,
                _ => 24
            };
            DueDate = CreatedAt.AddHours(hours);
        }
    }
}
