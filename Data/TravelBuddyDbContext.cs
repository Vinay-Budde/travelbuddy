using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Models;
using ModelRoute = TravelBuddy.Models.Route;

namespace TravelBuddy.Data
{
    public class TravelBuddyDbContext : IdentityDbContext<ApplicationUser>
    {
        public TravelBuddyDbContext(DbContextOptions<TravelBuddyDbContext> options)
            : base(options)
        {
        }

        public DbSet<Operator> Operators => Set<Operator>();
        public DbSet<ModelRoute> Routes => Set<ModelRoute>();
        public DbSet<Stop> Stops => Set<Stop>();
        public DbSet<RouteStop> RouteStops => Set<RouteStop>();
        public DbSet<FareProduct> FareProducts => Set<FareProduct>();
        public DbSet<FareProductRoute> FareProductRoutes => Set<FareProductRoute>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Pass> Passes => Set<Pass>();
        public DbSet<QrCredential> QrCredentials => Set<QrCredential>();
        public DbSet<BoardingValidation> BoardingValidations => Set<BoardingValidation>();
        public DbSet<Refund> Refunds => Set<Refund>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // FareProductRoute composite key
            builder.Entity<FareProductRoute>()
                .HasKey(fpr => new { fpr.FareProductId, fpr.RouteId });

            builder.Entity<FareProductRoute>()
                .HasOne(fpr => fpr.FareProduct)
                .WithMany(fp => fp.FareProductRoutes)
                .HasForeignKey(fpr => fpr.FareProductId);

            builder.Entity<FareProductRoute>()
                .HasOne(fpr => fpr.Route)
                .WithMany(r => r.FareProductRoutes)
                .HasForeignKey(fpr => fpr.RouteId);

            // RouteStop unique RouteId + StopSequence
            builder.Entity<RouteStop>()
                .HasIndex(rs => new { rs.RouteId, rs.StopSequence })
                .IsUnique();

            // QrCredential TokenHash index
            builder.Entity<QrCredential>()
                .HasIndex(q => q.TokenHash)
                .IsUnique();

            // Pass PassNumber unique index
            builder.Entity<Pass>()
                .HasIndex(p => p.PassNumber)
                .IsUnique();

            // Order OrderNumber unique index
            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            // Route RouteCode unique
            builder.Entity<ModelRoute>()
                .HasIndex(r => r.RouteCode)
                .IsUnique();

            // Stop StopCode unique
            builder.Entity<Stop>()
                .HasIndex(s => s.StopCode)
                .IsUnique();

            // Operator unique code
            builder.Entity<Operator>()
                .HasIndex(o => o.OperatorCode)
                .IsUnique();
        }
    }
}

