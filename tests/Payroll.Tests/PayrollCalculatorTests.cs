using Payroll.Domain.Calculation;
using Payroll.Domain.Enums;

namespace Payroll.Tests;

public class PayrollCalculatorTests
{
    private readonly PayrollCalculator _calculator = new();

    private PayBreakdown Calc(PayInput input) => _calculator.Calculate(input, TaxTable.Default, PayrollRates.Default);

    [Fact]
    public void Salaried_gross_is_annual_salary_divided_by_twelve()
    {
        var result = Calc(new PayInput { PayType = PayType.Salaried, AnnualSalary = 60000m });

        Assert.Equal(5000m, result.Gross);
        Assert.Equal(0m, result.RegularHours);
        Assert.Equal(0m, result.OvertimeHours);
    }

    [Fact]
    public void Salaried_full_breakdown_with_pension_and_other_deductions()
    {
        var result = Calc(new PayInput
        {
            PayType = PayType.Salaried,
            AnnualSalary = 60000m,
            PensionOptIn = true,
            OtherDeductions = 120m
        });

        // 0% on first 1000, 15% on 3000, 25% on 1000
        Assert.Equal(700m, result.IncomeTax);
        Assert.Equal(250m, result.SocialContribution);
        Assert.Equal(150m, result.Pension);
        Assert.Equal(120m, result.OtherDeductions);
        Assert.Equal(3780m, result.Net);
        Assert.Equal(result.Gross - result.TotalDeductions, result.Net);
    }

    [Fact]
    public void Pension_is_zero_when_employee_did_not_opt_in()
    {
        var result = Calc(new PayInput { PayType = PayType.Salaried, AnnualSalary = 24000m, PensionOptIn = false });

        Assert.Equal(0m, result.Pension);
        Assert.Equal(2000m, result.Gross);
        Assert.Equal(150m, result.IncomeTax);
        Assert.Equal(100m, result.SocialContribution);
        Assert.Equal(1750m, result.Net);
    }

    [Fact]
    public void Salaried_gross_is_rounded_to_cents()
    {
        var result = Calc(new PayInput { PayType = PayType.Salaried, AnnualSalary = 50000m });

        Assert.Equal(4166.67m, result.Gross);
    }

    [Fact]
    public void Hourly_pay_uses_overtime_premium_per_week()
    {
        var result = Calc(new PayInput
        {
            PayType = PayType.Hourly,
            HourlyRate = 20m,
            WeeklyHours = new[] { 45m, 40m, 38m, 42m }
        });

        Assert.Equal(158m, result.RegularHours);
        Assert.Equal(7m, result.OvertimeHours);
        // 158 * 20 + 7 * 20 * 1.5
        Assert.Equal(3370m, result.Gross);
        Assert.Equal(355.5m, result.IncomeTax);
        Assert.Equal(168.5m, result.SocialContribution);
        Assert.Equal(2846m, result.Net);
    }

    [Fact]
    public void Hourly_without_hours_has_zero_pay()
    {
        var result = Calc(new PayInput { PayType = PayType.Hourly, HourlyRate = 25m });

        Assert.Equal(0m, result.Gross);
        Assert.Equal(0m, result.Net);
    }

    [Fact]
    public void Low_income_is_not_taxed()
    {
        var result = Calc(new PayInput { PayType = PayType.Salaried, AnnualSalary = 9600m });

        Assert.Equal(800m, result.Gross);
        Assert.Equal(0m, result.IncomeTax);
        Assert.Equal(40m, result.SocialContribution);
    }

    [Fact]
    public void Missing_salary_throws()
    {
        Assert.Throws<InvalidOperationException>(() => Calc(new PayInput { PayType = PayType.Salaried }));
    }

    [Fact]
    public void Missing_hourly_rate_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Calc(new PayInput { PayType = PayType.Hourly, WeeklyHours = new[] { 40m } }));
    }

    [Fact]
    public void Custom_rates_are_applied()
    {
        var rates = new PayrollRates(0.10m, 0.05m, 2m, 35m);
        var result = _calculator.Calculate(new PayInput
        {
            PayType = PayType.Hourly,
            HourlyRate = 10m,
            WeeklyHours = new[] { 40m },
            PensionOptIn = true
        }, TaxTable.Default, rates);

        Assert.Equal(35m, result.RegularHours);
        Assert.Equal(5m, result.OvertimeHours);
        Assert.Equal(450m, result.Gross);
        Assert.Equal(45m, result.SocialContribution);
        Assert.Equal(22.5m, result.Pension);
    }
}
