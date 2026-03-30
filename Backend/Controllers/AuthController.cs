using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        ApplicationDbContext context,
        ILogger<AuthController> logger,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { error = "Email and password are required" });
            }

            // Check if user exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new { error = "Email already registered" });
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLower(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                EmailVerified = true, // Set to true for testing
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Email}", user.Email);

            return Ok(new
            {
                success = true,
                message = "Registration successful!",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request?.Email);
            return StatusCode(500, new { error = "Registration failed. Please try again." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (user == null)
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate a simple token (in production, use JWT)
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Id.ToString()));

            _logger.LogInformation("User logged in: {Email}", user.Email);

            return Ok(new
            {
                success = true,
                token = token,
                userId = user.Id,
                email = user.Email,
                expiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request?.Email);
            return StatusCode(500, new { error = "Login failed. Please try again." });
        }
    }
}