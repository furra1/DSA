using DSAManager.Backend.Application.DTOs;
using DSAManager.Backend.Domain.Entities;
using DSAManager.Backend.Domain.Enums;

namespace DSAManager.Backend.Application.Interfaces;

public interface IUserService
{
    Task<(User? User, string? Error)> RegisterEmployeeAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<(User? User, string? Error)> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);

    Task<User?> ValidateCredentialsAsync(
        string login,
        string password,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<(bool Success, string? Error)> ChangeRoleAsync(
        int userId,
        UserRole newRole,
        CancellationToken cancellationToken);
}