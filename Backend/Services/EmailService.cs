using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Backend.Helpers;

namespace Backend.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string to, string name)
    {
        var body = $@"
            <h1>Welcome to Subscription Tracker!</h1>
            <p>Hi {name},</p>
            <p>Thank you for joining Subscription Tracker. We'll help you keep track of all your subscriptions and save money.</p>
            <p>Get started by adding your first subscription to see your monthly spending.</p>
            <p>Best regards,<br/>The Subscription Tracker Team</p>
        ";

        await SendEmailAsync(to, "Welcome to Subscription Tracker!", body);
    }

    public async Task SendSubscriptionReminderAsync(string to, string subscriptionName, decimal amount, DateTime chargeDate)
    {
        var body = $@"
            <h1>Subscription Reminder</h1>
            <p>Your subscription <strong>{subscriptionName}</strong> will be charged <strong>${amount}</strong> on <strong>{chargeDate:MMMM d, yyyy}</strong>.</p>
            <p>If you no longer use this service, click here to cancel.</p>
            <p>Best regards,<br/>The Subscription Tracker Team</p>
        ";

        await SendEmailAsync(to, $"Reminder: {subscriptionName} renewing soon", body);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken)
    {
        var resetLink = $"{_emailSettings.AppUrl}/reset-password?token={resetToken}";
        var body = $@"
            <h1>Password Reset Request</h1>
            <p>Click the link below to reset your password:</p>
            <p><a href='{resetLink}'>{resetLink}</a></p>
            <p>This link expires in 1 hour.</p>
            <p>If you didn't request this, ignore this email.</p>
        ";

        await SendEmailAsync(to, "Password Reset Request", body);
    }

    public async Task SendEmailVerificationAsync(string to, string verificationToken)
    {
        var verifyLink = $"{_emailSettings.AppUrl}/verify-email?token={verificationToken}";
        var body = $@"
            <h1>Verify Your Email</h1>
            <p>Click the link below to verify your email address:</p>
            <p><a href='{verifyLink}'>{verifyLink}</a></p>
            <p>This link expires in 24 hours.</p>
        ";

        await SendEmailAsync(to, "Verify Your Email", body);
    }
}