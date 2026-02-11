using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceCore.Models
{
    public class SolutionAttachment
    {
        public int Id { get; set; }

        public int SolutionId { get; set; }
        public Solution? Solution { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
