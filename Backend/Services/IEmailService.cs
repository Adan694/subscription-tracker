namespace Backend.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendWelcomeEmailAsync(string to, string name);
    Task SendSubscriptionReminderAsync(string to, string subscriptionName, decimal amount, DateTime chargeDate);
    Task SendPasswordResetEmailAsync(string to, string resetToken);
    Task SendEmailVerificationAsync(string to, string verificationToken);
}