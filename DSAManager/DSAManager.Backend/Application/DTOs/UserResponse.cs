using DSAManager.Backend.Domain.Entities;
using DSAManager.Backend.Domain.Enums;

namespace DSAManager.Backend.Application.DTOs;

public sealed record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string EmployeeNumber,
    UserRole Role,
    int? GroupId,
    bool IsActive,
    DateTime CreatedAt)
{
    public static UserResponse FromUser(User user)
    {
        return new UserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.EmployeeNumber,
            user.Role,
            user.GroupId,
            user.IsActive,
            user.CreatedAt);
    }
}