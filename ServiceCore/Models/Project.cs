using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class Project
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(150)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } // Planning, Active, On Hold, Completed

        [StringLength(50)]
        public string? Priority { get; set; } // Low, Medium, High, Critical

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        // This replaces the simple "Lead" string with a relationship if desired, 
        // but keeping the string as a fallback or display name is fine too. 
        // We will add an OwnerId for the relationship.
        public string? Lead { get; set; }

        public int? OwnerId { get; set; }
        public User? Owner { get; set; }

        public int? TeamLeadId { get; set; }
        public User? TeamLead { get; set; }

        public ICollection<User> TeamMembers { get; set; } = new List<User>();

        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    }
}
