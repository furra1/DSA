using System.ComponentModel.DataAnnotations;

namespace DSAManager.Backend.Application.DTOs;

public class LoginRequest
{
    [Required]
    [StringLength(255)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}