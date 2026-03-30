using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Services;

public class ReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public ReminderService(IServiceScopeFactory scopeFactory, ILogger<ReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var remindersToSend = await dbContext.Subscriptions
            .Include(s => s.User)
            .Where(s => s.IsActive &&
                        s.NextChargeDate <= DateTime.UtcNow.AddDays(3) &&
                        s.NextChargeDate > DateTime.UtcNow &&
                        (s.LastReminderSentAt == null || s.LastReminderSentAt < DateTime.UtcNow.AddDays(-1)))
            .ToListAsync();

        foreach (var subscription in remindersToSend)
        {
            try
            {
                if (subscription.User != null && !string.IsNullOrEmpty(subscription.User.Email))
                {
                    await emailService.SendSubscriptionReminderAsync(
                        subscription.User.Email,
                        subscription.Name,
                        subscription.Amount,
                        subscription.NextChargeDate
                    );

                    subscription.LastReminderSentAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation("Reminder sent for subscription {SubscriptionName}", subscription.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for subscription {SubscriptionId}", subscription.Id);
            }
        }
    }
}