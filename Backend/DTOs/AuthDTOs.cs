using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }
}

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}