using DSAManager.Backend.Application.DTOs;
using DSAManager.Backend.Domain.Entities;
using DSAManager.Backend.Domain.Enums;
using DSAManager.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DSAManager.Backend.Application.Services;

public class UserService(AppDbContext db, IPasswordHasher<User> hasher)
{
    public async Task<(User? User, string? Error)> CreateAsync(RegisterRequest request, UserRole role, CancellationToken ct, int? groupId = null)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var employee = request.EmployeeNumber.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email || u.EmployeeNumber == employee, ct))
            return (null, "Email или табельный номер уже зарегистрирован.");
        var user = new User {
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Email = email, EmployeeNumber = employee, Role = role, GroupId = groupId,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return (user, null);
    }

    public async Task<User?> ValidateAsync(string login, string password, CancellationToken ct)
    {
        var key = login.Trim();
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == key.ToLower() || u.EmployeeNumber == key, ct);
        if (user is null || !user.IsActive) return null;
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded) {
            user.PasswordHash = hasher.HashPassword(user, password);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public Task<bool> HasAnyUsersAsync(CancellationToken ct) => db.Users.AnyAsync(ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(ct);

    public async Task<(User? User, string? Error)> CreateAdminUserAsync(CreateUserRequest r, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(r.Role, true, out var role))
            return (null, "Неизвестная роль.");
        return await CreateAsync(new RegisterRequest(r.FirstName, r.LastName, r.Email, r.EmployeeNumber, r.Password), role, ct, r.GroupId);
    }
}
