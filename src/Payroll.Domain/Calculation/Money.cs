namespace Payroll.Domain.Calculation;

public static class Money
{
    /// <summary>Rounds a monetary amount to cents, half-up (away from zero).</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static decimal RoundHours(decimal hours) => Math.Round(hours, 2, MidpointRounding.AwayFromZero);
}
