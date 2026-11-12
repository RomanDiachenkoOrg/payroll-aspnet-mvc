namespace Payroll.Domain.Calculation;

public readonly record struct HoursSplit(decimal Regular, decimal Overtime)
{
    public decimal Total => Regular + Overtime;

    public static HoursSplit operator +(HoursSplit a, HoursSplit b) =>
        new(a.Regular + b.Regular, a.Overtime + b.Overtime);
}

public static class OvertimeCalculator
{
    /// <summary>
    /// Splits the hours worked in a single week into regular and overtime hours.
    /// Overtime is every hour above the weekly threshold.
    /// </summary>
    public static HoursSplit SplitWeek(decimal weeklyHours, decimal threshold = 40m)
    {
        if (weeklyHours < 0) throw new ArgumentOutOfRangeException(nameof(weeklyHours));

        if (weeklyHours > threshold)
            return new HoursSplit(threshold, weeklyHours - threshold);

        return new HoursSplit(weeklyHours, 0m);
    }

    public static HoursSplit SplitWeeks(IEnumerable<decimal> weeklyHours, decimal threshold = 40m) =>
        weeklyHours.Aggregate(new HoursSplit(0, 0), (acc, h) => acc + SplitWeek(h, threshold));
}
