using Payroll.Domain.Abstractions;
using Payroll.Domain.Calculation;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Services;

public interface IPayRunService
{
    Task<IReadOnlyList<PayRun>> GetAllAsync(CancellationToken ct = default);
    Task<PayRun?> GetAsync(int id, CancellationToken ct = default);
    Task<OperationResult<PayRun>> CreateAsync(DateTime periodStart, DateTime periodEnd, DateTime payDate, string? name, CancellationToken ct = default);
    Task<OperationResult> CalculateAsync(int id, CancellationToken ct = default);
    Task<OperationResult> ApproveAsync(int id, CancellationToken ct = default);
    Task<OperationResult> MarkPaidAsync(int id, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default);
    Task<Payslip?> GetPayslipAsync(int payslipId, CancellationToken ct = default);
    Task<IReadOnlyList<Payslip>> GetPayslipsForEmployeeAsync(int employeeId, bool finalisedOnly, CancellationToken ct = default);
}

public sealed class PayRunService : IPayRunService
{
    private readonly IPayRunRepository _payRuns;
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISettingsRepository _settings;
    private readonly IPayrollCalculator _calculator;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public PayRunService(
        IPayRunRepository payRuns,
        IEmployeeRepository employees,
        ITimesheetRepository timesheets,
        ISettingsRepository settings,
        IPayrollCalculator calculator,
        IUnitOfWork uow,
        IAuditService audit,
        ICurrentUser user,
        IClock clock)
    {
        _payRuns = payRuns;
        _employees = employees;
        _timesheets = timesheets;
        _settings = settings;
        _calculator = calculator;
        _uow = uow;
        _audit = audit;
        _user = user;
        _clock = clock;
    }

    public Task<IReadOnlyList<PayRun>> GetAllAsync(CancellationToken ct = default) => _payRuns.GetAllAsync(ct);

    public Task<PayRun?> GetAsync(int id, CancellationToken ct = default) => _payRuns.GetByIdAsync(id, includePayslips: true, ct);

    public async Task<OperationResult<PayRun>> CreateAsync(DateTime periodStart, DateTime periodEnd, DateTime payDate, string? name, CancellationToken ct = default)
    {
        periodStart = periodStart.Date;
        periodEnd = periodEnd.Date;

        if (periodEnd < periodStart)
            return OperationResult<PayRun>.Failure("The period end must be on or after the period start.", "PeriodEnd");
        if ((periodEnd - periodStart).TotalDays > 31)
            return OperationResult<PayRun>.Failure("A pay run period cannot be longer than one month.", "PeriodEnd");
        if (payDate.Date < periodStart)
            return OperationResult<PayRun>.Failure("The pay date cannot be before the period start.", "PayDate");
        if (await _payRuns.OverlapsAsync(periodStart, periodEnd, null, ct))
            return OperationResult<PayRun>.Failure("This period overlaps an existing pay run.", "PeriodStart");

        var payRun = new PayRun
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Payroll {periodStart:MMMM yyyy}" : name.Trim(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PayDate = payDate.Date,
            Status = PayRunStatus.Draft,
            CreatedAt = _clock.UtcNow,
            CreatedBy = _user.Email
        };

        _payRuns.Add(payRun);
        await _uow.SaveChangesAsync(ct);
        _audit.Record(AuditActions.Create, nameof(PayRun), payRun.Id, payRun.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult<PayRun>.Success(payRun);
    }

    public async Task<OperationResult> CalculateAsync(int id, CancellationToken ct = default)
    {
        var payRun = await _payRuns.GetByIdAsync(id, includePayslips: true, ct);
        if (payRun is null) return OperationResult.Failure("Pay run not found.");
        if (payRun.Status is not (PayRunStatus.Draft or PayRunStatus.Calculated))
            return OperationResult.Failure($"A pay run in status {payRun.Status} cannot be recalculated.");

        var settings = await _settings.GetSettingsAsync(ct);
        var brackets = await _settings.GetTaxBracketsAsync(ct);
        var taxTable = new TaxTable(brackets.Select(b => new TaxBand(b.LowerBound, b.UpperBound, b.Rate)));
        var rates = new PayrollRates(settings.SocialContributionRate, settings.PensionRate,
            settings.OvertimeMultiplier, settings.WeeklyOvertimeThreshold);

        var employees = await _employees.GetPayableAsync(payRun.PeriodStart, payRun.PeriodEnd, ct);
        var timesheets = await _timesheets.GetApprovedForPeriodAsync(payRun.PeriodStart, payRun.PeriodEnd, ct);
        var hoursByEmployee = timesheets
            .GroupBy(t => t.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<decimal>)g.Select(t => t.TotalHours).ToList());

        _payRuns.RemovePayslips(payRun.Payslips.ToList());
        payRun.Payslips.Clear();

        var skipped = 0;
        foreach (var employee in employees)
        {
            var input = new PayInput
            {
                PayType = employee.PayType,
                AnnualSalary = employee.AnnualSalary,
                HourlyRate = employee.HourlyRate,
                WeeklyHours = hoursByEmployee.GetValueOrDefault(employee.Id, Array.Empty<decimal>()),
                PensionOptIn = employee.PensionOptIn,
                OtherDeductions = employee.MonthlyOtherDeductions
            };

            if (employee.PayType == PayType.Hourly && input.WeeklyHours.Count == 0)
            {
                skipped++;
                continue;
            }

            var result = _calculator.Calculate(input, taxTable, rates);
            payRun.Payslips.Add(new Payslip
            {
                EmployeeId = employee.Id,
                PayType = employee.PayType,
                AnnualSalary = employee.AnnualSalary,
                HourlyRate = employee.HourlyRate,
                RegularHours = result.RegularHours,
                OvertimeHours = result.OvertimeHours,
                Gross = result.Gross,
                IncomeTax = result.IncomeTax,
                SocialContribution = result.SocialContribution,
                Pension = result.Pension,
                OtherDeductions = result.OtherDeductions,
                Net = result.Net
            });
        }

        payRun.Status = PayRunStatus.Calculated;
        payRun.CalculatedAt = _clock.UtcNow;
        _audit.Record(AuditActions.Calculate, nameof(PayRun), id,
            $"{payRun.Payslips.Count} payslips, gross {payRun.TotalGross:0.00}" + (skipped > 0 ? $", {skipped} hourly without approved hours" : ""));
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ApproveAsync(int id, CancellationToken ct = default)
    {
        var payRun = await _payRuns.GetByIdAsync(id, includePayslips: true, ct);
        if (payRun is null) return OperationResult.Failure("Pay run not found.");
        if (payRun.Status != PayRunStatus.Calculated)
            return OperationResult.Failure("Only calculated pay runs can be approved.");
        if (payRun.Payslips.Count == 0)
            return OperationResult.Failure("The pay run has no payslips.");

        payRun.Status = PayRunStatus.Approved;
        payRun.ApprovedAt = _clock.UtcNow;
        payRun.ApprovedBy = _user.Email;
        _audit.Record(AuditActions.Approve, nameof(PayRun), id, payRun.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> MarkPaidAsync(int id, CancellationToken ct = default)
    {
        var payRun = await _payRuns.GetByIdAsync(id, includePayslips: false, ct);
        if (payRun is null) return OperationResult.Failure("Pay run not found.");
        if (payRun.Status != PayRunStatus.Approved)
            return OperationResult.Failure("Only approved pay runs can be marked as paid.");

        payRun.Status = PayRunStatus.Paid;
        payRun.PaidAt = _clock.UtcNow;
        _audit.Record(AuditActions.MarkPaid, nameof(PayRun), id, payRun.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var payRun = await _payRuns.GetByIdAsync(id, includePayslips: true, ct);
        if (payRun is null) return OperationResult.Failure("Pay run not found.");
        if (payRun.Status is PayRunStatus.Approved or PayRunStatus.Paid)
            return OperationResult.Failure("Approved or paid pay runs cannot be deleted.");

        _payRuns.Remove(payRun);
        _audit.Record(AuditActions.Delete, nameof(PayRun), id, payRun.Name);
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public Task<Payslip?> GetPayslipAsync(int payslipId, CancellationToken ct = default) =>
        _payRuns.GetPayslipAsync(payslipId, ct);

    public Task<IReadOnlyList<Payslip>> GetPayslipsForEmployeeAsync(int employeeId, bool finalisedOnly, CancellationToken ct = default) =>
        _payRuns.GetPayslipsForEmployeeAsync(employeeId, finalisedOnly, ct);
}
