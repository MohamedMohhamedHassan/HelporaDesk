using System;

namespace ServiceCore.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string? Action { get; set; } // e.g., "Created Ticket", "Moved Task"
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
