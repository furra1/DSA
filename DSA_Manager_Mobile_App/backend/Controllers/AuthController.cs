using DSAManager.API.Services;
using DSAManager.Backend.Application.DTOs;
using DSAManager.Backend.Application.Services;
using DSAManager.Backend.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DSAManager.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(UserService users, JwtTokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Укажите логин и пароль." });
        var user = await users.ValidateAsync(request.Login, request.Password, ct);
        if (user is null) return Unauthorized(new { message = "Неверный логин или пароль." });
        var token = tokens.Create(user);
        return Ok(new AuthResponse(token.Token, token.ExpiresAt, ToResponse(user)));
    }


    // One-time bootstrap: creates the first administrator only while the Users table is empty.
    [HttpPost("bootstrap-admin")]
    public async Task<IActionResult> BootstrapAdmin(RegisterRequest request, CancellationToken ct)
    {
        if (await users.HasAnyUsersAsync(ct))
            return Conflict(new { message = "Первый администратор уже создан. Используйте обычный вход." });
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.EmployeeNumber) ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 10)
            return BadRequest(new { message = "Заполните все поля; пароль — не менее 10 символов." });
        var result = await users.CreateAsync(request, UserRole.Admin, ct);
        if (result.User is null) return Conflict(new { message = result.Error });
        var token = tokens.Create(result.User);
        return StatusCode(201, new AuthResponse(token.Token, token.ExpiresAt, ToResponse(result.User)));
    }

    // Self-registration deliberately creates Employee only. Admin creation is a separate protected route.
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.EmployeeNumber) ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 10)
            return BadRequest(new { message = "Заполните обязательные поля; пароль — не менее 10 символов." });
        var result = await users.CreateAsync(request, UserRole.Employee, ct);
        if (result.User is null) return Conflict(new { message = result.Error });
        var token = tokens.Create(result.User);
        return StatusCode(201, new AuthResponse(token.Token, token.ExpiresAt, ToResponse(result.User)));
    }

    internal static UserResponse ToResponse(DSAManager.Backend.Domain.Entities.User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.EmployeeNumber, u.Role.ToString(), u.GroupId, u.IsActive, u.CreatedAt);
}
