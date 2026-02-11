using System;

namespace ServiceCore.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? LinkAction { get; set; } // URL or Action to click

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
