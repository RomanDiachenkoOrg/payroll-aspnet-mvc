using Payroll.Domain.Enums;

namespace Payroll.Domain.Calculation;

public sealed record PayrollRates(
    decimal SocialContributionRate,
    decimal PensionRate,
    decimal OvertimeMultiplier,
    decimal WeeklyOvertimeThreshold)
{
    public static PayrollRates Default { get; } = new(0.05m, 0.03m, 1.5m, 40m);
}

public sealed record PayInput
{
    public PayType PayType { get; init; }
    public decimal? AnnualSalary { get; init; }
    public decimal? HourlyRate { get; init; }
    /// <summary>Total approved hours per week inside the pay period (hourly staff only).</summary>
    public IReadOnlyList<decimal> WeeklyHours { get; init; } = Array.Empty<decimal>();
    public bool PensionOptIn { get; init; }
    public decimal OtherDeductions { get; init; }
}

public sealed record PayBreakdown(
    decimal RegularHours,
    decimal OvertimeHours,
    decimal Gross,
    decimal IncomeTax,
    decimal SocialContribution,
    decimal Pension,
    decimal OtherDeductions,
    decimal Net)
{
    public decimal TotalDeductions => IncomeTax + SocialContribution + Pension + OtherDeductions;
}

public interface IPayrollCalculator
{
    PayBreakdown Calculate(PayInput input, TaxTable taxTable, PayrollRates rates);
}

public sealed class PayrollCalculator : IPayrollCalculator
{
    public PayBreakdown Calculate(PayInput input, TaxTable taxTable, PayrollRates rates)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(taxTable);
        ArgumentNullException.ThrowIfNull(rates);

        decimal regularHours = 0m, overtimeHours = 0m, gross;

        switch (input.PayType)
        {
            case PayType.Salaried:
                if (input.AnnualSalary is null or <= 0)
                    throw new InvalidOperationException("Salaried employees need a positive annual salary.");
                gross = Money.Round(input.AnnualSalary.Value / 12m);
                break;

            case PayType.Hourly:
                if (input.HourlyRate is null or <= 0)
                    throw new InvalidOperationException("Hourly employees need a positive hourly rate.");
                var split = OvertimeCalculator.SplitWeeks(input.WeeklyHours, rates.WeeklyOvertimeThreshold);
                regularHours = split.Regular;
                overtimeHours = split.Overtime;
                var rate = input.HourlyRate.Value;
                gross = Money.Round(rate * regularHours + rate * rates.OvertimeMultiplier * overtimeHours);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(input), input.PayType, "Unknown pay type.");
        }

        var tax = taxTable.ComputeTax(gross);
        var social = decimal.Round(gross * rates.SocialContributionRate, 2);
        var pension = input.PensionOptIn ? Money.Round(gross * rates.PensionRate) : 0m;
        var other = Money.Round(Math.Max(0m, input.OtherDeductions));

        var net = Money.Round(gross - tax - social - pension - other);

        return new PayBreakdown(regularHours, overtimeHours, gross, tax, social, pension, other, net);
    }
}
