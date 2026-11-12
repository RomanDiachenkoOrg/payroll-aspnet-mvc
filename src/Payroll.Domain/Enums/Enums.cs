namespace Payroll.Domain.Enums;

public enum PayType
{
    Salaried = 0,
    Hourly = 1
}

public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum PayRunStatus
{
    Draft = 0,
    Calculated = 1,
    Approved = 2,
    Paid = 3
}

public enum UserRole
{
    Employee = 0,
    Manager = 1,
    Admin = 2
}
