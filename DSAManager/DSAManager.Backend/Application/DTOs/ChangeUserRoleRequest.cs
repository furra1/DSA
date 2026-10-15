using DSAManager.Backend.Domain.Enums;

namespace DSAManager.Backend.Application.DTOs;

public class ChangeUserRoleRequest
{
    public UserRole Role { get; set; }
}