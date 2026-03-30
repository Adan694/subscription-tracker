using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateSubscriptionRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }

    public string Frequency { get; set; } = "monthly";

    public DateTime? NextChargeDate { get; set; }

    [Url]
    public string? CancellationLink { get; set; }

    public string? Category { get; set; }

    public string? Notes { get; set; }
}

public class UpdateSubscriptionRequestDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? Amount { get; set; }

    public string? Frequency { get; set; }

    public DateTime? NextChargeDate { get; set; }

    [Url]
    public string? CancellationLink { get; set; }

    public string? Category { get; set; }

    public string? Notes { get; set; }

    public bool? IsActive { get; set; }
}

public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public DateTime NextChargeDate { get; set; }
    public string? CancellationLink { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Calculated fields
    public decimal MonthlyEquivalent { get; set; }
    public decimal YearlyEquivalent { get; set; }
    public int DaysUntilNextCharge { get; set; }
}

public class DashboardResponseDto
{
    public List<SubscriptionResponseDto> Subscriptions { get; set; } = new();
    public decimal TotalMonthlySpend { get; set; }
    public decimal TotalYearlySpend { get; set; }
    public int ActiveSubscriptionCount { get; set; }
    public List<UpcomingChargeDto> UpcomingCharges { get; set; } = new();
    public Dictionary<string, decimal> SpendingByCategory { get; set; } = new();
}

public class UpcomingChargeDto
{
    public Guid SubscriptionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChargeDate { get; set; }
    public int DaysUntilCharge { get; set; }
}