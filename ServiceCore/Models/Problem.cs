using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceCore.Models
{
    public class Problem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Impact { get; set; } = "Low"; // Low, Medium, High, Extensive
        
        [StringLength(50)]
        public string Urgency { get; set; } = "Low"; // Low, Medium, High, Critical
        
        [StringLength(50)]
        public string Priority { get; set; } = "Low"; // Low, Medium, High, Critical

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open"; // Draft, Open, Under Investigation, Root Cause Identified, Known Error, Resolved, Closed

        // RCA Fields
        public string? RootCause { get; set; }
        public string? Workaround { get; set; }
        public string? PermanentFix { get; set; }
        public string? RCAMethod { get; set; } // 5 Whys, Fishbone, etc.
        public string? InvestigationNotes { get; set; }

        public int? CategoryId { get; set; }
        public TicketCategory? Category { get; set; }

        public int? AssetId { get; set; }
        public Asset? Asset { get; set; }

        public int? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public User? AssignedTo { get; set; }

        public int CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public User? Creator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // Navigation properties
        public ICollection<ProblemIncident> LinkedIncidents { get; set; } = new List<ProblemIncident>();
        public ICollection<ProblemActivity> Activities { get; set; } = new List<ProblemActivity>();
    }

    public class ProblemIncident
    {
        public int Id { get; set; }
        
        public int ProblemId { get; set; }
        public Problem? Problem { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.Now;
    }

    public class ProblemActivity
    {
        public int Id { get; set; }
        
        public int ProblemId { get; set; }
        public Problem? Problem { get; set; }

        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
