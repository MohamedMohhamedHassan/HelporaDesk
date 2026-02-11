using System;

namespace ServiceCore.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int ProjectTaskId { get; set; }
        public ProjectTask? ProjectTask { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
