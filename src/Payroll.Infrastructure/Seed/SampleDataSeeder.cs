using Payroll.Domain.Abstractions;
using Payroll.Domain.Calculation;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Seed;

/// <summary>
/// Creates a realistic demo company. All dates are relative to "today" so the
/// data always looks current: two paid months, an open draft for this month.
/// </summary>
internal sealed class SampleDataSeeder
{
    public const string DemoPassword = "Passw0rd!";

    private readonly PayrollDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPayrollCalculator _calculator;
    private readonly DateTime _today;
    private readonly Random _random = new(20240601);

    public SampleDataSeeder(PayrollDbContext db, IPasswordHasher hasher, IPayrollCalculator calculator, DateTime today)
    {
        _db = db;
        _hasher = hasher;
        _calculator = calculator;
        _today = today.Date;
    }

    private sealed record EmployeeSeed(
        string First, string Last, string Dept, string Title, PayType PayType, decimal Amount,
        int HiredMonthsAgo, bool Pension, bool Active = true, decimal Health = 120m, decimal Union = 0m);

    private static readonly EmployeeSeed[] Employees =
    {
        new("Morgan", "Reyes", "ENG", "Engineering Manager", PayType.Salaried, 118000m, 74, true),
        new("Jordan", "Patel", "ENG", "Software Developer (Contract)", PayType.Hourly, 52.50m, 20, false, Health: 0m),
        new("Priya", "Shah", "ENG", "Senior Software Engineer", PayType.Salaried, 104000m, 51, true),
        new("Lucas", "Becker", "ENG", "Software Engineer", PayType.Salaried, 86500m, 26, true),
        new("Hannah", "Kim", "ENG", "Product Designer", PayType.Salaried, 72000m, 18, false),
        new("Diego", "Alvarez", "ENG", "DevOps Engineer", PayType.Salaried, 95000m, 33, true),
        new("Sofia", "Romano", "ENG", "Junior Developer", PayType.Hourly, 31.00m, 7, false, Health: 0m),
        new("Ethan", "Walsh", "ENG", "Software Engineer", PayType.Salaried, 84000m, 40, true, Active: false),
        new("Grace", "Okafor", "FIN", "Finance Director", PayType.Salaried, 126000m, 90, true),
        new("Samuel", "Novak", "FIN", "Senior Accountant", PayType.Salaried, 78000m, 44, true),
        new("Mia", "Fischer", "FIN", "Payroll Specialist", PayType.Salaried, 61000m, 15, true),
        new("Oliver", "Grant", "FIN", "Accounts Clerk", PayType.Hourly, 24.75m, 9, false, Health: 0m),
        new("Isabella", "Moreau", "SAL", "Head of Sales", PayType.Salaried, 112000m, 62, true),
        new("Noah", "Jensen", "SAL", "Account Executive", PayType.Salaried, 68000m, 28, false),
        new("Ava", "Costa", "SAL", "Account Executive", PayType.Salaried, 66500m, 12, true),
        new("Liam", "OBrien", "SAL", "Sales Development Rep", PayType.Hourly, 22.00m, 5, false, Health: 0m),
        new("Chloe", "Dubois", "SAL", "Sales Coordinator", PayType.Salaried, 49000m, 30, false, Active: false),
        new("Henry", "Lindqvist", "OPS", "Operations Manager", PayType.Salaried, 92000m, 58, true, Union: 0m),
        new("Zoe", "Martins", "OPS", "Warehouse Lead", PayType.Hourly, 27.40m, 36, true, Union: 15m),
        new("Jack", "Murphy", "OPS", "Warehouse Associate", PayType.Hourly, 19.80m, 14, false, Health: 0m, Union: 15m),
        new("Emma", "Novak", "OPS", "Logistics Coordinator", PayType.Salaried, 54000m, 22, true),
        new("Ryan", "Cho", "OPS", "Driver", PayType.Hourly, 21.25m, 25, false, Union: 15m, Active: false),
        new("Olivia", "Brennan", "HR", "HR Director", PayType.Salaried, 99000m, 80, true),
        new("Ella", "Nakamura", "HR", "HR Generalist", PayType.Salaried, 58000m, 19, true),
        new("Mason", "Ward", "HR", "Recruiter", PayType.Salaried, 56000m, 11, false)
    };

    public async Task SeedAsync(CancellationToken ct)
    {
        var settings = new PayrollSettings { UpdatedAt = _today.AddMonths(-6), UpdatedBy = "admin@acme.test" };
        _db.PayrollSettings.Add(settings);

        var brackets = new List<TaxBracket>
        {
            new() { LowerBound = 0m, UpperBound = 1000m, Rate = 0m, SortOrder = 1 },
            new() { LowerBound = 1000m, UpperBound = 4000m, Rate = 0.15m, SortOrder = 2 },
            new() { LowerBound = 4000m, UpperBound = null, Rate = 0.25m, SortOrder = 3 }
        };
        _db.TaxBrackets.AddRange(brackets);

        var health = new DeductionType { Code = "HEALTH", Name = "Health insurance", DefaultAmount = 120m };
        var dental = new DeductionType { Code = "DENTAL", Name = "Dental plan", DefaultAmount = 25m };
        var union = new DeductionType { Code = "UNION", Name = "Union dues", DefaultAmount = 15m };
        _db.DeductionTypes.AddRange(health, dental, union);

        var departments = new Dictionary<string, Department>
        {
            ["ENG"] = new() { Name = "Engineering", Code = "ENG", CostCenter = "CC-100", Description = "Product development and infrastructure" },
            ["FIN"] = new() { Name = "Finance", Code = "FIN", CostCenter = "CC-200", Description = "Accounting, payroll and treasury" },
            ["SAL"] = new() { Name = "Sales", Code = "SAL", CostCenter = "CC-300", Description = "New business and account management" },
            ["OPS"] = new() { Name = "Operations", Code = "OPS", CostCenter = "CC-400", Description = "Warehouse, logistics and facilities" },
            ["HR"] = new() { Name = "HR", Code = "HR", CostCenter = "CC-500", Description = "People operations and recruiting" }
        };
        foreach (var d in departments.Values) d.CreatedAt = _today.AddYears(-8);
        _db.Departments.AddRange(departments.Values);
        await _db.SaveChangesAsync(ct);

        var employees = new List<Employee>();
        var number = 1001;
        foreach (var seed in Employees)
        {
            var hireDate = _today.AddMonths(-seed.HiredMonthsAgo).AddDays(-_random.Next(0, 20));
            var employee = new Employee
            {
                EmployeeNumber = $"E{number++:D4}",
                FirstName = seed.First,
                LastName = seed.Last == "OBrien" ? "O'Brien" : seed.Last,
                Email = $"{seed.First.ToLowerInvariant()}.{seed.Last.ToLowerInvariant()}@acme.test",
                Phone = $"+1 555 01{_random.Next(10, 99)} {_random.Next(1000, 9999)}",
                JobTitle = seed.Title,
                HireDate = hireDate,
                DepartmentId = departments[seed.Dept].Id,
                PayType = seed.PayType,
                AnnualSalary = seed.PayType == PayType.Salaried ? seed.Amount : null,
                HourlyRate = seed.PayType == PayType.Hourly ? seed.Amount : null,
                PensionOptIn = seed.Pension,
                IsActive = seed.Active,
                TerminationDate = seed.Active ? null : _today.AddMonths(-3).AddDays(-_random.Next(0, 25))
            };

            if (seed.Health > 0) employee.Deductions.Add(new EmployeeDeduction { DeductionType = health, Amount = seed.Health });
            if (seed.Union > 0) employee.Deductions.Add(new EmployeeDeduction { DeductionType = union, Amount = seed.Union });
            if (seed.Pension && seed.PayType == PayType.Salaried && seed.Amount > 100000m)
                employee.Deductions.Add(new EmployeeDeduction { DeductionType = dental, Amount = 25m });

            AddSalaryHistory(employee, seed);
            employees.Add(employee);
        }

        _db.Employees.AddRange(employees);
        await _db.SaveChangesAsync(ct);

        var byEmail = employees.ToDictionary(e => e.Email);
        var manager = byEmail["morgan.reyes@acme.test"];
        var staff = byEmail["jordan.patel@acme.test"];

        _db.Users.AddRange(
            new AppUser
            {
                Email = "admin@acme.test", DisplayName = "Alex Admin", Role = UserRole.Admin,
                PasswordHash = _hasher.Hash(DemoPassword), CreatedAt = _today.AddYears(-3),
                LastLoginAt = _today.AddDays(-1).AddHours(9)
            },
            new AppUser
            {
                Email = "manager@acme.test", DisplayName = manager.FullName, Role = UserRole.Manager,
                EmployeeId = manager.Id, DepartmentId = manager.DepartmentId,
                PasswordHash = _hasher.Hash(DemoPassword), CreatedAt = _today.AddYears(-2),
                LastLoginAt = _today.AddDays(-2).AddHours(14)
            },
            new AppUser
            {
                Email = "employee@acme.test", DisplayName = staff.FullName, Role = UserRole.Employee,
                EmployeeId = staff.Id, DepartmentId = staff.DepartmentId,
                PasswordHash = _hasher.Hash(DemoPassword), CreatedAt = staff.HireDate,
                LastLoginAt = _today.AddDays(-3).AddHours(17)
            },
            new AppUser
            {
                Email = "grace.okafor@acme.test", DisplayName = "Grace Okafor", Role = UserRole.Manager,
                EmployeeId = byEmail["grace.okafor@acme.test"].Id, DepartmentId = departments["FIN"].Id,
                PasswordHash = _hasher.Hash(Guid.NewGuid().ToString("N")), CreatedAt = _today.AddYears(-4),
                IsActive = false
            });

        SeedTimesheets(employees);
        await _db.SaveChangesAsync(ct);

        await SeedPayRunsAsync(employees, settings, brackets, ct);
        SeedAuditLog(employees);
        await _db.SaveChangesAsync(ct);
    }

    private void AddSalaryHistory(Employee employee, EmployeeSeed seed)
    {
        // Walk back from the current pay: one raise per full year of service (max 3).
        var years = Math.Min(3, seed.HiredMonthsAgo / 12);
        var amounts = new List<decimal> { seed.Amount };
        for (var i = 0; i < years; i++)
        {
            var previous = amounts[^1] / 1.04m;
            amounts.Add(seed.PayType == PayType.Salaried ? Math.Round(previous / 500m) * 500m : Math.Round(previous, 2));
        }
        amounts.Reverse();

        for (var i = 0; i < amounts.Count; i++)
        {
            var effective = i == 0 ? employee.HireDate : employee.HireDate.AddYears(i);
            employee.SalaryHistory.Add(new SalaryHistory
            {
                EffectiveDate = effective,
                PayType = seed.PayType,
                AnnualSalary = seed.PayType == PayType.Salaried ? amounts[i] : null,
                HourlyRate = seed.PayType == PayType.Hourly ? amounts[i] : null,
                Reason = i == 0 ? "Hire" : "Annual review",
                ChangedBy = "admin@acme.test",
                CreatedAt = effective
            });
        }
    }

    private void SeedTimesheets(List<Employee> employees)
    {
        var currentWeek = DateHelper.StartOfWeek(_today);
        var monthStart = DateHelper.StartOfMonth(_today);
        var firstWeek = DateHelper.StartOfWeek(monthStart.AddMonths(-2));
        var hourly = employees.Where(e => e.PayType == PayType.Hourly).ToList();

        foreach (var employee in hourly)
        {
            for (var week = firstWeek; week <= currentWeek; week = week.AddDays(7))
            {
                if (week < employee.HireDate) continue;
                if (!employee.IsActive && week > employee.TerminationDate) continue;

                var status = TimesheetStatus.Approved;
                string? rejection = null;

                if (week >= monthStart || week == currentWeek)
                {
                    var weeksAgo = (int)((currentWeek - week).TotalDays / 7);
                    status = weeksAgo switch
                    {
                        0 => TimesheetStatus.Draft,
                        1 => employee.Email.StartsWith("zoe") ? TimesheetStatus.Rejected : TimesheetStatus.Submitted,
                        _ => TimesheetStatus.Approved
                    };
                    if (status == TimesheetStatus.Rejected)
                        rejection = "Saturday hours were not pre-approved. Please correct and resubmit.";
                    if (status == TimesheetStatus.Draft && !employee.Email.StartsWith("jordan") && !employee.Email.StartsWith("jack"))
                        continue;
                }

                var timesheet = new Timesheet
                {
                    EmployeeId = employee.Id,
                    WeekStart = week,
                    Status = status,
                    RejectionReason = rejection,
                    CreatedAt = week.AddDays(4).AddHours(16),
                    SubmittedAt = status is TimesheetStatus.Draft ? null : week.AddDays(4).AddHours(17),
                    ReviewedAt = status is TimesheetStatus.Approved or TimesheetStatus.Rejected ? week.AddDays(7).AddHours(10) : null,
                    ReviewedBy = status is TimesheetStatus.Approved or TimesheetStatus.Rejected ? ReviewerFor(employee) : null
                };

                for (var d = 0; d < 7; d++)
                {
                    decimal hours = 0;
                    if (d < 5) hours = 8m;
                    if (d == 4 && _random.NextDouble() < 0.35) hours += _random.Next(1, 5);
                    if (d == 5 && _random.NextDouble() < 0.25) hours = _random.Next(2, 7);
                    if (status == TimesheetStatus.Draft && week.AddDays(d) > _today) hours = 0;
                    timesheet.Entries.Add(new TimesheetEntry { WorkDate = week.AddDays(d), Hours = hours });
                }

                _db.Timesheets.Add(timesheet);
            }
        }
    }

    private static string ReviewerFor(Employee employee) =>
        employee.Email.StartsWith("jordan") || employee.Email.StartsWith("sofia") ? "manager@acme.test" : "admin@acme.test";

    private async Task SeedPayRunsAsync(List<Employee> employees, PayrollSettings settings, List<TaxBracket> brackets, CancellationToken ct)
    {
        var taxTable = new TaxTable(brackets.Select(b => new TaxBand(b.LowerBound, b.UpperBound, b.Rate)));
        var rates = new PayrollRates(settings.SocialContributionRate, settings.PensionRate, settings.OvertimeMultiplier, settings.WeeklyOvertimeThreshold);
        var currentMonth = DateHelper.StartOfMonth(_today);

        for (var offset = -2; offset <= 0; offset++)
        {
            var start = currentMonth.AddMonths(offset);
            var end = DateHelper.EndOfMonth(start);
            var payDate = DateHelper.PayDateFor(start, settings.PayDayOfMonth);
            var run = new PayRun
            {
                Name = $"Payroll {start:MMMM yyyy}",
                PeriodStart = start,
                PeriodEnd = end,
                PayDate = payDate,
                CreatedAt = start.AddDays(20),
                CreatedBy = "admin@acme.test",
                Status = offset < 0 ? PayRunStatus.Paid : PayRunStatus.Draft
            };

            if (offset < 0)
            {
                run.CalculatedAt = payDate.AddDays(-4);
                run.ApprovedAt = payDate.AddDays(-3);
                run.ApprovedBy = "admin@acme.test";
                run.PaidAt = payDate;

                var approved = _db.Timesheets.Local
                    .Where(t => t.Status == TimesheetStatus.Approved && t.WeekStart >= start && t.WeekStart <= end)
                    .GroupBy(t => t.EmployeeId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<decimal>)g.Select(t => t.TotalHours).ToList());

                foreach (var employee in employees.Where(e => e.HireDate <= end && (e.TerminationDate is null || e.TerminationDate >= start)))
                {
                    var hours = approved.GetValueOrDefault(employee.Id, Array.Empty<decimal>());
                    if (employee.PayType == PayType.Hourly && hours.Count == 0) continue;

                    var result = _calculator.Calculate(new PayInput
                    {
                        PayType = employee.PayType,
                        AnnualSalary = employee.AnnualSalary,
                        HourlyRate = employee.HourlyRate,
                        WeeklyHours = hours,
                        PensionOptIn = employee.PensionOptIn,
                        OtherDeductions = employee.Deductions.Sum(d => d.Amount)
                    }, taxTable, rates);

                    run.Payslips.Add(new Payslip
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
            }

            _db.PayRuns.Add(run);
        }

        await _db.SaveChangesAsync(ct);
    }

    private void SeedAuditLog(List<Employee> employees)
    {
        var runs = _db.PayRuns.Local.OrderBy(r => r.PeriodStart).ToList();
        var entries = new List<AuditLog>();

        foreach (var run in runs)
        {
            entries.Add(Entry(run.CreatedAt, "admin@acme.test", "Create", nameof(PayRun), run.Id, run.Name));
            if (run.Status == PayRunStatus.Paid)
            {
                entries.Add(Entry(run.CalculatedAt!.Value.AddHours(9), "admin@acme.test", "Calculate", nameof(PayRun), run.Id,
                    $"{run.Payslips.Count} payslips, gross {run.TotalGross:0.00}"));
                entries.Add(Entry(run.ApprovedAt!.Value.AddHours(11), "admin@acme.test", "Approve", nameof(PayRun), run.Id, run.Name));
                entries.Add(Entry(run.PaidAt!.Value.AddHours(15), "admin@acme.test", "MarkPaid", nameof(PayRun), run.Id, run.Name));
            }
        }

        foreach (var t in _db.Timesheets.Local.Where(t => t.ReviewedAt.HasValue).OrderBy(t => t.ReviewedAt).TakeLast(12))
        {
            var action = t.Status == TimesheetStatus.Rejected ? "Reject" : "Approve";
            entries.Add(Entry(t.ReviewedAt!.Value, t.ReviewedBy!, action, nameof(Timesheet), t.Id, t.RejectionReason));
        }

        foreach (var e in employees.Where(e => !e.IsActive))
            entries.Add(Entry(e.TerminationDate!.Value.AddHours(10), "admin@acme.test", "Deactivate", nameof(Employee), e.Id, e.FullName));

        var newest = employees.OrderByDescending(e => e.HireDate).First();
        entries.Add(Entry(newest.HireDate.AddDays(-7).AddHours(13), "admin@acme.test", "Create", nameof(Employee), newest.Id, $"{newest.EmployeeNumber} {newest.FullName}"));
        entries.Add(Entry(_today.AddDays(-30).AddHours(16), "admin@acme.test", "SalaryChange", nameof(Employee), employees[2].Id, "100000.00 -> 104000.00"));
        entries.Add(Entry(_today.AddDays(-1).AddHours(9), "admin@acme.test", "Login", nameof(AppUser), 1, null));
        entries.Add(Entry(_today.AddDays(-2).AddHours(14), "manager@acme.test", "Login", nameof(AppUser), 2, null));
        entries.Add(Entry(_today.AddDays(-2).AddHours(18), "unknown@acme.test", "LoginFailed", nameof(AppUser), null, null));

        _db.AuditLogs.AddRange(entries.OrderBy(e => e.Timestamp));
    }

    private static AuditLog Entry(DateTime at, string user, string action, string type, object? id, string? details) => new()
    {
        Timestamp = at.ToUniversalTime(),
        UserEmail = user,
        Action = action,
        EntityType = type,
        EntityId = id?.ToString(),
        Details = details
    };
}
