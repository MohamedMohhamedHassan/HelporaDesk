using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace ServiceCore.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        public string? PasswordHash { get; set; }

        [StringLength(50)]
        public string? Role { get; set; } = "User"; // User, Agent, Admin

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public string? InviteToken { get; set; }
        
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public ICollection<Ticket> SubmittedTickets { get; set; } = new List<Ticket>();
        public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
        public ICollection<Project> JoinedProjects { get; set; } = new List<Project>();
        public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();

        // Helper method to hash password
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Helper method to verify password
        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(PasswordHash)) return false;
            var hash = HashPassword(password);
            return hash == PasswordHash;
        }
    }
}
