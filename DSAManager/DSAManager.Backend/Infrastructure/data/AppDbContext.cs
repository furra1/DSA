using DSAManager.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DSAManager.Backend.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.HasKey(u => u.Id);

        user.Property(u => u.Id)
            .HasColumnName("id");

        user.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        user.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        user.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        user.HasIndex(u => u.Email)
            .IsUnique();

        user.Property(u => u.EmployeeNumber)
            .HasColumnName("employee_number")
            .HasMaxLength(50)
            .IsRequired();

        user.HasIndex(u => u.EmployeeNumber)
            .IsUnique();

        user.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        user.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        user.Property(u => u.GroupId)
            .HasColumnName("group_id");

        user.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        user.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        user.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}