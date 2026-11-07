using DSAManager.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DSAManager.Backend.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Plan> Plans => Set<Plan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.ToTable("Users", "dsa"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("UserId").ValueGeneratedOnAdd();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.Property(x => x.EmployeeNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Role).HasColumnName("RoleCode").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.TwoFactorSecret).HasColumnType("varbinary(512)");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmployeeNumber).IsUnique();
            e.HasOne(x => x.Group).WithMany(x => x.Users).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Group>(e =>
        {
            e.ToTable("Groups", "dsa"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("GroupId");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("Products", "dsa"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("ProductId");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks", "dsa"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("TaskId");
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            e.Property(x => x.ProgressCompleted).HasDefaultValue(0);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Group).WithMany(x => x.Tasks).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Plan>(e =>
        {
            e.ToTable("Plans", "dsa"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("PlanId");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.PeriodType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Group).WithMany(x => x.Plans).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
