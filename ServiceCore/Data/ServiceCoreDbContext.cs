using Microsoft.EntityFrameworkCore;
using ServiceCore.Models;

namespace ServiceCore.Data
{
    public class ServiceCoreDbContext : DbContext
    {
        public ServiceCoreDbContext(DbContextOptions<ServiceCoreDbContext> options) : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<KanbanCard> KanbanCards { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<AssetCategory> AssetCategories { get; set; } = null!;
        public DbSet<AssetAssignment> AssetAssignments { get; set; } = null!;
        public DbSet<AssetMaintenance> AssetMaintenances { get; set; } = null!;
        public DbSet<AssetHistory> AssetHistories { get; set; } = null!;
        public DbSet<Approval> Approvals { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        
        public DbSet<TicketCategory> TicketCategories { get; set; } = null!;
        public DbSet<TicketPriority> TicketPriorities { get; set; } = null!;
        public DbSet<TicketStatus> TicketStatuses { get; set; } = null!;
        public DbSet<TicketComment> TicketComments { get; set; } = null!;
        public DbSet<TicketAttachment> TicketAttachments { get; set; } = null!;
        public DbSet<ServiceCore.Models.Asset> Assets { get; set; } = null!;
        public DbSet<ServiceCore.Models.Setting> Settings { get; set; } = null!;
        
        public DbSet<ProjectTask> ProjectTasks { get; set; } = null!;
        public DbSet<Milestone> Milestones { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Project Owner (One-to-Many)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany() // Assuming User doesn't need a collection of OwnedProjects explicitly
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project Team Lead (One-to-Many)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.TeamLead)
                .WithMany()
                .HasForeignKey(p => p.TeamLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project Team Members (Many-to-Many)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.TeamMembers)
                .WithMany(u => u.JoinedProjects)
                .UsingEntity(j => j.ToTable("ProjectTeamMembers"));

            // Project Task Assignees (Many-to-Many)
            modelBuilder.Entity<ProjectTask>()
                .HasMany(t => t.Assignees)
                .WithMany(u => u.AssignedTasks)
                .UsingEntity(j => j.ToTable("TaskAssignees"));
            
            // Legacy AssigneeId (Optional: Restrict delete)
            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket Relationships
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Requester)
                .WithMany(u => u.SubmittedTickets)
                .HasForeignKey(t => t.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Assigned)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Priority)
                .WithMany()
                .HasForeignKey(t => t.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Status)
                .WithMany()
                .HasForeignKey(t => t.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Asset Financial Properties
            modelBuilder.Entity<ServiceCore.Models.Asset>()
                .Property(a => a.PurchaseCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ServiceCore.Models.Asset>()
                .Property(a => a.ResidualValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<AssetMaintenance>()
                .Property(m => m.Cost)
                .HasPrecision(18, 2);
        }
    }
}
