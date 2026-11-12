namespace Payroll.Domain.Common;

public static class DateHelper
{
    /// <summary>Returns the Monday of the week that contains <paramref name="date"/>.</summary>
    public static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    public static DateTime StartOfMonth(DateTime date) => new(date.Year, date.Month, 1);

    public static DateTime EndOfMonth(DateTime date) => StartOfMonth(date).AddMonths(1).AddDays(-1);

    /// <summary>Pay date for a month: the configured day, or the last day of a shorter month.</summary>
    public static DateTime PayDateFor(DateTime monthStart, int payDay)
    {
        var day = Math.Min(payDay, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
        return new DateTime(monthStart.Year, monthStart.Month, day);
    }
}
