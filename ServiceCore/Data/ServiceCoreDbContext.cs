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
        public DbSet<TicketLink> TicketLinks { get; set; } = null!;
        public DbSet<Problem> Problems { get; set; } = null!;
        public DbSet<ProblemIncident> ProblemIncidents { get; set; } = null!;
        public DbSet<ProblemActivity> ProblemActivities { get; set; } = null!;
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

        // Solutions Module
        public DbSet<Solution> Solutions { get; set; } = null!;
        public DbSet<SolutionTopic> SolutionTopics { get; set; } = null!;
        public DbSet<SolutionAttachment> SolutionAttachments { get; set; } = null!;

        // Contract Management Module
        public DbSet<Vendor> Vendors { get; set; } = null!;
        public DbSet<ContractType> ContractTypes { get; set; } = null!;
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<ContractAttachment> ContractAttachments { get; set; } = null!;
        public DbSet<ContractApproval> ContractApprovals { get; set; } = null!;
        public DbSet<ContractPayment> ContractPayments { get; set; } = null!;
        public DbSet<ContractHistory> ContractHistories { get; set; } = null!;

        // Change Management Module
        public DbSet<ChangeRequest> ChangeRequests { get; set; } = null!;
        public DbSet<ChangeApproval> ChangeApprovals { get; set; } = null!;
        public DbSet<ChangeTask> ChangeTasks { get; set; } = null!;
        public DbSet<ChangeActivity> ChangeActivities { get; set; } = null!;
        public DbSet<ChangeAsset> ChangeAssets { get; set; } = null!;

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
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Assigned)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedId)
                .OnDelete(DeleteBehavior.ClientSetNull);

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

            modelBuilder.Entity<TicketComment>()
                .HasOne(tc => tc.User)
                .WithMany()
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Problem>()
                .HasOne(p => p.Asset)
                .WithMany(a => a.Problems)
                .HasForeignKey(p => p.AssetId)
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

            // Solution Relationships
            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Topic)
                .WithMany(t => t.Solutions)
                .HasForeignKey(s => s.TopicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Owner)
                .WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solution>()
                .HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SolutionTopic>()
                .HasOne(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(t => t.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // TicketCategory Hierarchical Relationship
            modelBuilder.Entity<TicketCategory>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Contract Module Configuration
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.ContractType)
                .WithMany(t => t.Contracts)
                .HasForeignKey(c => c.ContractTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Vendor)
                .WithMany(v => v.Contracts)
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .Property(c => c.Value)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ContractPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ContractApproval>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractHistory>()
                .HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Asset -> Contract Relationship
            modelBuilder.Entity<ServiceCore.Models.Asset>()
                .HasOne(a => a.Contract)
                .WithMany()
                .HasForeignKey(a => a.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project -> Contract Relationship
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Contract)
                .WithMany()
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket Links
            modelBuilder.Entity<TicketLink>()
                .HasOne(tl => tl.SourceTicket)
                .WithMany(t => t.LinkedFrom)
                .HasForeignKey(tl => tl.SourceTicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketLink>()
                .HasOne(tl => tl.TargetTicket)
                .WithMany(t => t.LinkedTo)
                .HasForeignKey(tl => tl.TargetTicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // Problem Module
            modelBuilder.Entity<Problem>()
                .HasOne(p => p.AssignedTo)
                .WithMany()
                .HasForeignKey(p => p.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Problem>()
                .HasOne(p => p.Creator)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProblemIncident>()
                .HasOne(pi => pi.Problem)
                .WithMany(p => p.LinkedIncidents)
                .HasForeignKey(pi => pi.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProblemIncident>()
                .HasOne(pi => pi.Ticket)
                .WithMany()
                .HasForeignKey(pi => pi.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // Change Management Relationships
            modelBuilder.Entity<ChangeRequest>()
                .HasOne(c => c.RequestedBy)
                .WithMany()
                .HasForeignKey(c => c.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChangeRequest>()
                .HasOne(c => c.AssignedTo)
                .WithMany()
                .HasForeignKey(c => c.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChangeApproval>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChangeTask>()
                .HasOne(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChangeAsset>()
                .HasOne(ca => ca.Asset)
                .WithMany()
                .HasForeignKey(ca => ca.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
