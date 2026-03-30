using Backend.Models;
using Backend.DTOs;

namespace Backend.Services;

public interface ITokenService
{
    LoginResponseDto GenerateTokens(User user);
    bool ValidateRefreshToken(string refreshToken);
    string GenerateEmailVerificationToken();
    string GeneratePasswordResetToken();
}