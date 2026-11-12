using Payroll.Domain.Calculation;
using Payroll.Domain.Common;

namespace Payroll.Tests;

public class OvertimeCalculatorTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(32, 32, 0)]
    [InlineData(40, 40, 0)]
    [InlineData(40.5, 40, 0.5)]
    [InlineData(52, 40, 12)]
    public void Splits_week_at_forty_hours(decimal hours, decimal regular, decimal overtime)
    {
        var split = OvertimeCalculator.SplitWeek(hours);

        Assert.Equal(regular, split.Regular);
        Assert.Equal(overtime, split.Overtime);
    }

    [Fact]
    public void Overtime_is_calculated_per_week_not_per_month()
    {
        // 30 + 50 = 80 hours, but only the second week has overtime.
        var split = OvertimeCalculator.SplitWeeks(new[] { 30m, 50m });

        Assert.Equal(70m, split.Regular);
        Assert.Equal(10m, split.Overtime);
    }

    [Fact]
    public void Negative_hours_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OvertimeCalculator.SplitWeek(-1m));
    }
}

public class DateHelperTests
{
    [Theory]
    [InlineData("2024-06-03", "2024-06-03")] // Monday
    [InlineData("2024-06-05", "2024-06-03")] // Wednesday
    [InlineData("2024-06-09", "2024-06-03")] // Sunday
    public void StartOfWeek_returns_monday(string date, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), DateHelper.StartOfWeek(DateTime.Parse(date)));
    }

    [Fact]
    public void PayDate_is_clamped_to_month_length()
    {
        Assert.Equal(new DateTime(2025, 2, 28), DateHelper.PayDateFor(new DateTime(2025, 2, 1), 30));
        Assert.Equal(new DateTime(2025, 3, 28), DateHelper.PayDateFor(new DateTime(2025, 3, 1), 28));
    }
}
