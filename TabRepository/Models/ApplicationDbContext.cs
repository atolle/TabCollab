using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TabRepository.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Stripe;

namespace TabRepository.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            // Projects Table
            builder.Entity<Project>()
                .HasMany(p => p.Albums)
                .WithOne(t => t.Project)
                .OnDelete(DeleteBehavior.Cascade);

            // Albums Table
            builder.Entity<Album>()
                .HasMany(p => p.Tabs)
                .WithOne(t => t.Album)
                .OnDelete(DeleteBehavior.Cascade);

            // Tabs Table
            builder.Entity<Tab>()
                .HasMany(t => t.TabVersions)
                .WithOne(t => t.Tab)
                .OnDelete(DeleteBehavior.Cascade);

            // Tab Versions Table
            // TabVersion and TabFile is a 1:1 relationship so TabFile's PK is also a FK pointing to
            // TabVersion's PK
            builder.Entity<TabVersion>()
                .HasOne(v => v.TabFile)
                .WithOne(v => v.TabVersion)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey<TabFile>(f => f.Id);

            // Friends Table
            builder.Entity<Friend>()
                .HasKey(c => new { c.User1Id, c.User2Id });

            builder.Entity<Friend>()
                .HasOne(f => f.User1)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Friend>()
                .HasOne(f => f.User2)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Contirbutors Table
            builder.Entity<ProjectContributor>()
                .HasKey(p => new { p.UserId, p.ProjectId });

            builder.Entity<ProjectContributor>()
                .HasOne(p => p.User)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectContributor>()
                .HasOne(p => p.Project)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserTabVersion>()
                .HasKey(v => new { v.UserId, v.TabId });

            // Stripe models
            builder.Entity<StripeProduct>()
                .HasMany(p => p.Plans)
                .WithOne(p => p.Product)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StripePlan>()
                .HasMany(p => p.Subscriptions)
                .WithOne(p => p.Plan)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StripeSubscription>()
                .HasOne(s => s.Customer)
                .WithOne(c => c.Subscription)
                .HasForeignKey<StripeCustomer>(c => c.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<StripeCustomer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<Tab> Tabs { get; set; }
        public DbSet<TabVersion> TabVersions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TabFile> TabFiles { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<ProjectContributor> ProjectContributors { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationUser> NotificationUsers { get; set; }
        public DbSet<UserTabVersion> UserTabVersions { get; set; }
        public DbSet<StripeProduct> StripeProducts { get; set; }
        public DbSet<StripePlan> StripePlans { get; set; }
        public DbSet<StripeSubscription> StripeSubscriptions { get; set; }
        public DbSet<StripeCustomer> StripeCustomers { get; set; }

        public ApplicationDbContext()
            : base()
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}
