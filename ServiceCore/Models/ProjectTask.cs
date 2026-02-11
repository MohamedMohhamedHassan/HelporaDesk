using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } // To Do, In Progress, Review, Done

        [StringLength(50)]
        public string? Priority { get; set; } // Low, Medium, High

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        public ICollection<User> Assignees { get; set; } = new List<User>();

        public int? MilestoneId { get; set; }
        public Milestone? Milestone { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
