using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool EmailVerified { get; set; } = true; // Set to true by default for now

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}