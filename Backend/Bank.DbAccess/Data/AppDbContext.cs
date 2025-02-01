using Bank.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Bank.DbAccess.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ledger> Ledgers { get; init; }
    public DbSet<User> Users { get; init; }
    public DbSet<Booking> Bookings { get; init; }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the Booking entity
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
                
            entity.HasOne(e => e.Source)
                .WithMany()
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Destination)
                .WithMany()
                .HasForeignKey(e => e.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
            
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Role)
                .HasConversion<string>();
        });
    }
}