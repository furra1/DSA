using DSAManager.Backend.Application.DTOs;
using DSAManager.Backend.Application.Interfaces;
using DSAManager.Backend.Domain.Entities;
using DSAManager.Backend.Domain.Enums;
using DSAManager.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DSAManager.Backend.Application.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public Task<(User? User, string? Error)> RegisterEmployeeAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Обычная регистрация всегда создаёт Employee.
        return CreateInternalAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.EmployeeNumber,
            request.Password,
            UserRole.Employee,
            cancellationToken);
    }

    public Task<(User? User, string? Error)> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(UserRole), request.Role))
        {
            return Task.FromResult<(User?, string?)>(
                (null, "Указана неизвестная роль."));
        }

        return CreateInternalAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.EmployeeNumber,
            request.Password,
            request.Role,
            cancellationToken);
    }

    private async Task<(User? User, string? Error)> CreateInternalAsync(
        string firstName,
        string lastName,
        string email,
        string employeeNumber,
        string password,
        UserRole role,
        CancellationToken cancellationToken)
    {
        email = email.Trim().ToLowerInvariant();
        employeeNumber = employeeNumber.Trim();

        bool exists = await _db.Users.AnyAsync(
            u => u.Email == email ||
                 u.EmployeeNumber == employeeNumber,
            cancellationToken);

        if (exists)
        {
            return (null,
                "Пользователь с такой почтой или табельным номером уже существует.");
        }

        var user = new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            EmployeeNumber = employeeNumber,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password);

        _db.Users.Add(user);

        await _db.SaveChangesAsync(cancellationToken);

        return (user, null);
    }

    public async Task<User?> ValidateCredentialsAsync(
        string login,
        string password,
        CancellationToken cancellationToken)
    {
        string loginValue = login.Trim();
        string normalizedEmail = loginValue.ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == normalizedEmail ||
                 u.EmployeeNumber == loginValue,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                password);

            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == id,
                cancellationToken);
    }

    public async Task<(bool Success, string? Error)> ChangeRoleAsync(
        int userId,
        UserRole newRole,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(UserRole), newRole))
        {
            return (false, "Указана неизвестная роль.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == userId,
            cancellationToken);

        if (user is null)
        {
            return (false, "Пользователь не найден.");
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }
}