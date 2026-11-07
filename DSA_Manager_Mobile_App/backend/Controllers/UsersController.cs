using DSAManager.Backend.Application.DTOs;
using DSAManager.Backend.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSAManager.API.Controllers;

[ApiController, Route("api/admin/users"), Authorize(Roles = "Admin")]
public class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok((await users.GetAllAsync(ct)).Select(AuthController.ToResponse));

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        if (User.IsInRole("Manager") && !User.IsInRole("Admin") && !string.Equals(request.Role, "Employee", StringComparison.OrdinalIgnoreCase))
            return Forbid();
        var result = await users.CreateAdminUserAsync(request, ct);
        if (result.User is null) return Conflict(new { message = result.Error });
        return Created($"/api/users/{result.User.Id}", AuthController.ToResponse(result.User));
    }
}
