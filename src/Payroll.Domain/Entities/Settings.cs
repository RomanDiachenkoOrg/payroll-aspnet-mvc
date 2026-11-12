namespace Payroll.Domain.Entities;

public class TaxBracket
{
    public int Id { get; set; }
    public decimal LowerBound { get; set; }
    /// <summary>Null means "no upper limit".</summary>
    public decimal? UpperBound { get; set; }
    /// <summary>Rate as a fraction, e.g. 0.15 for 15%.</summary>
    public decimal Rate { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Singleton row with company-wide payroll parameters.</summary>
public class PayrollSettings
{
    public int Id { get; set; }
    public decimal SocialContributionRate { get; set; } = 0.05m;
    public decimal PensionRate { get; set; } = 0.03m;
    public decimal OvertimeMultiplier { get; set; } = 1.5m;
    public decimal WeeklyOvertimeThreshold { get; set; } = 40m;
    public int PayDayOfMonth { get; set; } = 28;
    public string Currency { get; set; } = "USD";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
