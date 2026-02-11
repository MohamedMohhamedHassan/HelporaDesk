using System;

namespace ServiceCore.Models
{
    public class Comment
    {
        public int Id { get; set; }
        
        public string? Content { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int ProjectTaskId { get; set; }
        public ProjectTask? ProjectTask { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
