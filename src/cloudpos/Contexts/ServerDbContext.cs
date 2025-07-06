using CloudInteractive.CloudPos.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Contexts;

public class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<TableSession> Sessions { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<TableSession>()
            .HasIndex(x => x.AuthCode)
            .IsUnique();
        
        builder.Entity<OrderItem>()
            .HasKey(x => new { x.OrderId, x.ItemId });
        
        builder.Entity<Category>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .IsRequired();

        builder.Entity<Order>()
            .HasMany(x => x.OrderItems)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired();

        builder.Entity<Item>()
            .HasMany(x => x.OrderItems)
            .WithOne(x => x.Item)
            .HasForeignKey(x => x.ItemId)
            .IsRequired();

        builder.Entity<Table>()
            .HasMany(x => x.Sessions)
            .WithOne(x => x.Table)
            .HasForeignKey(x => x.TableId)
            .IsRequired();

        builder.Entity<TableSession>()
            .HasMany(x => x.Orders)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .IsRequired();
    }
}