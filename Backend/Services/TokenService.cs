using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Backend.Models;
using Backend.DTOs;
using Backend.Helpers;

namespace Backend.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IOptions<JwtSettings> jwtSettings, ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public LoginResponseDto GenerateTokens(User user)
    {
        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        return new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
        };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("email_verified", user.EmailVerified.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateRefreshToken(string refreshToken)
    {
        return !string.IsNullOrEmpty(refreshToken) && refreshToken.Length > 20;
    }

    public string GenerateEmailVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    public string GeneratePasswordResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}