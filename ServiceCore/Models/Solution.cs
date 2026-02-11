using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class Solution
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public int TopicId { get; set; }
        public SolutionTopic? Topic { get; set; }

        public string? Keywords { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Approved, Published, Expired

        public int Views { get; set; } = 0;

        public int? OwnerId { get; set; }
        public User? Owner { get; set; }

        public int CreatedBy { get; set; }
        public User? Creator { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime? ReviewDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        public ICollection<SolutionAttachment> Attachments { get; set; } = new List<SolutionAttachment>();
    }
}
