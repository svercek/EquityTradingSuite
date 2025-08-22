using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EquityPerformanceTracker.Core.Models;

namespace EquityPerformanceTracker.Data.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
	public DbSet<Transaction> Transactions { get; set; } = null!;


        // Simple DbSets - no navigation properties for now
        public DbSet<Portfolio> Portfolios { get; set; } = null!;
        public DbSet<PortfolioHolding> PortfolioHoldings { get; set; } = null!;
        public DbSet<PerformanceSnapshot> PerformanceSnapshots { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // CRITICAL: Call base first
            base.OnModelCreating(modelBuilder);

            // Minimal configuration - just primary keys
            modelBuilder.Entity<Portfolio>().HasKey(e => e.Id);
            modelBuilder.Entity<PortfolioHolding>().HasKey(e => e.Id);
            modelBuilder.Entity<PerformanceSnapshot>().HasKey(e => e.Id);
            modelBuilder.Entity<Subscription>().HasKey(e => e.Id);

	        // Transaction Configuration
	        modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
    
                entity.HasOne(e => e.Portfolio)
                    .WithMany()
                    .HasForeignKey(e => e.PortfolioId)
                    .OnDelete(DeleteBehavior.Cascade);
          
                entity.HasOne(e => e.Holding)
                    .WithMany()
                    .HasForeignKey(e => e.HoldingId)
                    .OnDelete(DeleteBehavior.NoAction);
          
                // Indexes
                entity.HasIndex(e => e.PortfolioId);
                entity.HasIndex(e => e.HoldingId);
                entity.HasIndex(e => e.Symbol);
                entity.HasIndex(e => e.TransactionDate);
            });
        }
    }
}