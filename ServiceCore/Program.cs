using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCore.Data;
using Microsoft.AspNetCore.Identity;
using ServiceCore.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ServiceCore.Services.PermissionFilter>();
});
// Add authentication (cookie) and authorization
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
// Email Service - Gmail SMTP for real email delivery
builder.Services.AddScoped<ServiceCore.Services.IEmailService, ServiceCore.Services.SmtpEmailService>();
// Notification Service
builder.Services.AddScoped<ServiceCore.Services.INotificationService, ServiceCore.Services.NotificationService>();

// Register a password hasher for hashing user passwords when Identity isn't fully used
builder.Services.AddScoped<IPasswordHasher<ServiceCore.Models.User>, PasswordHasher<ServiceCore.Models.User>>();

// Configure EF Core with SQL Server. Update connection string in appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=.\\SQLEXPRESS;Database=ServiceCoreDb;Trusted_Connection=True;TrustServerCertificate=True";
builder.Services.AddDbContext<ServiceCoreDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created and seed basic data for development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServiceCoreDbContext>();
    // Get password hasher to create Identity-compatible password hashes for seeded users
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ServiceCore.Models.User>>();
    try
    {
        db.Database.EnsureCreated();

        // Seed Default Admin User if none exists
        if (!db.Users.Any(u => u.Email == "admin@servicecore.local"))
        {
            var adminUser = new ServiceCore.Models.User
            {
                Name = "Admin User",
                Email = "admin@servicecore.local",
                Role = "Admin",
                Department = "IT",
                IsActive = true
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "admin");
            db.Users.Add(adminUser);
        }

        // Seed Requested Admin User: m.hassan@gmail.com
        if (!db.Users.Any(u => u.Email == "m.hassan@gmail.com"))
        {
            var moUser = new ServiceCore.Models.User
            {
                Name = "Mohamed Hassan",
                Email = "m.hassan@gmail.com",
                Role = "Admin",
                Department = "Administration",
                IsActive = true
            };
            moUser.PasswordHash = passwordHasher.HashPassword(moUser, "admin");
            db.Users.Add(moUser);
        }
        db.SaveChanges();

        // FORCE seed Departments (added)
        var departments = new[] { "IT", "Engineering", "Sales", "Marketing", "HR" };
        foreach (var dept in departments)
        {
            if (!db.Departments.Any(d => d.Name == dept))
            {
                db.Departments.Add(new Department { Name = dept });
            }
        }
        db.SaveChanges();

        // Seed Categories
        var categories = new[] { "Hardware", "Software", "Network", "Access Request" };
        foreach (var cat in categories)
        {
            if (!db.TicketCategories.Any(c => c.Name == cat))
                db.TicketCategories.Add(new ServiceCore.Models.TicketCategory { Name = cat });
        }
        db.SaveChanges();

        // Seed Priorities
        var priorities = new[] { "Low", "Medium", "High", "Critical" };
        foreach (var prio in priorities)
        {
            if (!db.TicketPriorities.Any(p => p.Name == prio))
                db.TicketPriorities.Add(new ServiceCore.Models.TicketPriority { Name = prio });
        }
        db.SaveChanges();

        // Seed Statuses
        var statuses = new[] { "Open", "In Progress", "Resolved", "Closed" };
        foreach (var stat in statuses)
        {
            if (!db.TicketStatuses.Any(s => s.Name == stat))
                db.TicketStatuses.Add(new ServiceCore.Models.TicketStatus { Name = stat });
        }
        db.SaveChanges();

        // Seed Test Tickets if count is low
        if (db.Tickets.Count() < 3)
        {
            var adminUser = db.Users.First();
            var statusOpen = db.TicketStatuses.First(s => s.Name == "Open");
            var statusResolved = db.TicketStatuses.First(s => s.Name == "Resolved");
            var priorityHigh = db.TicketPriorities.First(p => p.Name == "High");
            var priorityMedium = db.TicketPriorities.First(p => p.Name == "Medium");
            var categorySoftware = db.TicketCategories.First(c => c.Name == "Software");
            var categoryNetwork = db.TicketCategories.First(c => c.Name == "Network");

            db.Tickets.AddRange(
                new ServiceCore.Models.Ticket
                {
                    Subject = "Network Outage - Building A",
                    Description = "Total loss of connectivity in the main lobby.",
                    StatusId = statusOpen.Id,
                    PriorityId = db.TicketPriorities.First(p => p.Name == "Critical").Id,
                    CategoryId = categoryNetwork.Id,
                    RequesterId = adminUser.Id,
                    CreatedAt = DateTime.Now.AddHours(-1)
                },
                new ServiceCore.Models.Ticket
                {
                    Subject = "Software License Request",
                    Description = "Need Adobe Creative Cloud for new designer.",
                    StatusId = statusOpen.Id,
                    PriorityId = priorityMedium.Id,
                    CategoryId = categorySoftware.Id,
                    RequesterId = adminUser.Id,
                    CreatedAt = DateTime.Now.AddHours(-12)
                },
                new ServiceCore.Models.Ticket
                {
                    Subject = "Printer Jam - Room 402",
                    Description = "Paper jam in HP LaserJet.",
                    StatusId = statusResolved.Id,
                    PriorityId = priorityMedium.Id,
                    CategoryId = db.TicketCategories.First(c => c.Name == "Hardware").Id,
                    RequesterId = adminUser.Id,
                    CreatedAt = DateTime.Now.AddDays(-1)
                }
            );

            db.Notifications.Add(new ServiceCore.Models.Notification
            {
                Title = "Database Seeded",
                Message = "System base data has been successfully initialized.",
                UserId = adminUser.Id,
                CreatedAt = DateTime.Now
            });

            db.SaveChanges();

            // Seed Asset Categories
            await AssetSeed.SeedAsync(db);
        }

        // Debug Logs
        Console.WriteLine($"[DEBUG] Database State:");
        Console.WriteLine($" - Users: {db.Users.Count()}");
        Console.WriteLine($" - Tickets: {db.Tickets.Count()}");
        Console.WriteLine($" - Categories: {db.TicketCategories.Count()}");
        Console.WriteLine($" - Priorities: {db.TicketPriorities.Count()}");
        Console.WriteLine($" - Statuses: {db.TicketStatuses.Count()}");
    }
    catch (Exception ex)
    {
        // Log error during seeding
        Console.WriteLine($"An error occurred seeding the DB: {ex.Message}");
    }
}

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
