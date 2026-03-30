using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers;

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
        // Get userId from middleware
        if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) || userIdObj is not Guid userId)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var subscriptions = await _context.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.NextChargeDate)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Amount,
                s.Frequency,
                s.NextChargeDate,
                s.CancellationLink
            })
            .ToListAsync();

        var monthlyTotal = subscriptions
            .Where(s => s.Frequency == "monthly")
            .Sum(s => s.Amount);

        return Ok(new
        {
            subscriptions = subscriptions,
            totalMonthlySpend = monthlyTotal,
            activeSubscriptionCount = subscriptions.Count
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDto request)
    {
        if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) || userIdObj is not Guid userId)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Amount = request.Amount,
            Frequency = request.Frequency ?? "monthly",
            NextChargeDate = request.NextChargeDate ?? DateTime.UtcNow.AddDays(30),
            CancellationLink = request.CancellationLink,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, subscriptionId = subscription.Id });
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
        if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) || userIdObj is not Guid userId)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
        {
            return NotFound(new { error = "Subscription not found" });
        }

        subscription.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
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