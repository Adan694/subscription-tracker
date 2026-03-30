using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }

    [Required]
    public string Frequency { get; set; } = "monthly"; // monthly, yearly, weekly

    public DateTime NextChargeDate { get; set; }

    [Url]
    public string? CancellationLink { get; set; }

    public string? Category { get; set; } // streaming, software, gym, etc.

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastReminderSentAt { get; set; }

    // Navigation property
    public User? User { get; set; }
}