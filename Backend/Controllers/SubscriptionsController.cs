using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(ApplicationDbContext context, ILogger<SubscriptionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscriptions()
    {
        // Get userId from the token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // If no claim, try to get from Authorization header
        if (string.IsNullOrEmpty(userIdClaim))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                try
                {
                    // Decode the Base64 token to get userId
                    var decodedBytes = Convert.FromBase64String(token);
                    var decodedUserId = System.Text.Encoding.UTF8.GetString(decodedBytes);

                    if (Guid.TryParse(decodedUserId, out var userId))
                    {
                        userIdClaim = decodedUserId;
                    }
                }
                catch
                {
                    // If token is not Base64, maybe it's the raw userId
                    if (Guid.TryParse(token, out var userId))
                    {
                        userIdClaim = token;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var finalUserId))
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        // Now get subscriptions for this user
        var subscriptions = await _context.Subscriptions
            .Where(s => s.UserId == finalUserId && s.IsActive)
            .OrderBy(s => s.NextChargeDate)
            .Select(s => new SubscriptionResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Amount = s.Amount,
                Frequency = s.Frequency,
                NextChargeDate = s.NextChargeDate,
                CancellationLink = s.CancellationLink,
                Category = s.Category,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                MonthlyEquivalent = s.Frequency == "monthly" ? s.Amount :
                                    s.Frequency == "yearly" ? s.Amount / 12 :
                                    s.Frequency == "weekly" ? s.Amount * 4.33m : s.Amount,
                YearlyEquivalent = s.Frequency == "monthly" ? s.Amount * 12 :
                                  s.Frequency == "yearly" ? s.Amount :
                                  s.Frequency == "weekly" ? s.Amount * 52 : s.Amount,
                DaysUntilNextCharge = (int)(s.NextChargeDate - DateTime.UtcNow).TotalDays
            })
            .ToListAsync();

        var dashboard = new DashboardResponseDto
        {
            Subscriptions = subscriptions,
            TotalMonthlySpend = subscriptions.Sum(s => s.MonthlyEquivalent),
            TotalYearlySpend = subscriptions.Sum(s => s.YearlyEquivalent),
            ActiveSubscriptionCount = subscriptions.Count,
            UpcomingCharges = subscriptions
                .Where(s => s.DaysUntilNextCharge <= 7)
                .Select(s => new UpcomingChargeDto
                {
                    SubscriptionId = s.Id,
                    Name = s.Name,
                    Amount = s.Amount,
                    ChargeDate = s.NextChargeDate,
                    DaysUntilCharge = s.DaysUntilNextCharge
                })
                .ToList(),
            SpendingByCategory = subscriptions
                .GroupBy(s => s.Category ?? "Other")
                .ToDictionary(g => g.Key, g => g.Sum(s => s.MonthlyEquivalent))
        };

        return Ok(dashboard);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        return Ok(new SubscriptionResponseDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Amount = subscription.Amount,
            Frequency = subscription.Frequency,
            NextChargeDate = subscription.NextChargeDate,
            CancellationLink = subscription.CancellationLink,
            Category = subscription.Category,
            Notes = subscription.Notes,
            CreatedAt = subscription.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDto request)
    {
        // Get userId from the token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // If no claim, try to get from Authorization header
        if (string.IsNullOrEmpty(userIdClaim))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                try
                {
                    var decodedBytes = Convert.FromBase64String(token);
                    var decodedUserId = System.Text.Encoding.UTF8.GetString(decodedBytes);
                    if (Guid.TryParse(decodedUserId, out var userId))
                    {
                        userIdClaim = decodedUserId;
                    }
                }
                catch
                {
                    if (Guid.TryParse(token, out var userId))
                    {
                        userIdClaim = token;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var finalUserId))
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        var subscription = new Subscription
        {
            UserId = finalUserId,
            Name = request.Name,
            Amount = request.Amount,
            Frequency = request.Frequency,
            NextChargeDate = request.NextChargeDate ?? DateTime.UtcNow.AddDays(30),
            CancellationLink = request.CancellationLink,
            Category = request.Category,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New subscription created: {Name} for user {UserId}", subscription.Name, finalUserId);

        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, new
        {
            success = true,
            subscriptionId = subscription.Id,
            message = "Subscription added successfully!"
        });
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequestDto request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        if (request.Name != null) subscription.Name = request.Name;
        if (request.Amount.HasValue) subscription.Amount = request.Amount.Value;
        if (request.Frequency != null) subscription.Frequency = request.Frequency;
        if (request.NextChargeDate.HasValue) subscription.NextChargeDate = request.NextChargeDate.Value;
        if (request.CancellationLink != null) subscription.CancellationLink = request.CancellationLink;
        if (request.Category != null) subscription.Category = request.Category;
        if (request.Notes != null) subscription.Notes = request.Notes;
        if (request.IsActive.HasValue) subscription.IsActive = request.IsActive.Value;

        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription updated: {Name} for user {UserId}", subscription.Name, userId);

        return Ok(new { success = true, message = "Subscription updated successfully!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        // Get userId from the token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                try
                {
                    var decodedBytes = Convert.FromBase64String(token);
                    var decodedUserId = System.Text.Encoding.UTF8.GetString(decodedBytes);
                    if (Guid.TryParse(decodedUserId, out var userId))
                    {
                        userIdClaim = decodedUserId;
                    }
                }
                catch
                {
                    if (Guid.TryParse(token, out var userId))
                    {
                        userIdClaim = token;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var finalUserId))
        {
            return Unauthorized(new { error = "Invalid user" });
        }

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == finalUserId);

        if (subscription == null)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        subscription.IsActive = false;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription deleted: {Name} for user {UserId}", subscription.Name, finalUserId);

        return Ok(new { success = true, message = "Subscription removed" });
    }
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            message = "API is working!",
            token = HttpContext.Request.Headers["Authorization"].ToString(),
            timestamp = DateTime.UtcNow
        });
    }
}