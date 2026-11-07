namespace DSAManager.Backend.Application.DTOs;
public sealed record LoginRequest(string Login, string Password);
public sealed record RegisterRequest(string FirstName, string LastName, string Email, string EmployeeNumber, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
public sealed record UserResponse(int Id, string FirstName, string LastName, string Email, string EmployeeNumber, string Role, int? GroupId, bool IsActive, DateTime CreatedAt);
public sealed record CreateUserRequest(string FirstName, string LastName, string Email, string EmployeeNumber, string Password, string Role = "Employee", int? GroupId = null);
