using System.Security.Claims;
using DSAManager.Backend.Application.DTOs;
using DSAManager.Backend.Application.Interfaces;
using DSAManager.Backend.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DSAManager.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(
            cancellationToken);

        return Ok(users.Select(UserResponse.FromUser));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(
            request,
            cancellationToken);

        if (result.User is null)
        {
            return Conflict(new { message = result.Error });
        }

        return Created(
            $"/api/users/{result.User.Id}",
            UserResponse.FromUser(result.User));
    }

    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(
        int id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        string? idClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(idClaim, out int currentUserId)
            && currentUserId == id
            && request.Role != UserRole.Admin)
        {
            return BadRequest(new
            {
                message =
                    "Нельзя снимать роль администратора со своего аккаунта этим запросом."
            });
        }

        var result = await _userService.ChangeRoleAsync(
            id,
            request.Role,
            cancellationToken);

        if (!result.Success)
        {
            return result.Error == "Пользователь не найден."
                ? NotFound(new { message = result.Error })
                : BadRequest(new { message = result.Error });
        }

        return NoContent();
    }
}