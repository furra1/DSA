using DSAManager.Backend.Domain.Enums;
namespace DSAManager.Backend.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string EmployeeNumber { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Employee;
    public int? GroupId { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public byte[]? TwoFactorSecret { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Group? Group { get; set; }
}
