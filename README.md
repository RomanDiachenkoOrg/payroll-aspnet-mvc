# Acme Payroll (ASP.NET Core MVC)

Payroll & HR web application for a small company: employees and departments, weekly
timesheets with an approval workflow, monthly pay runs with tax / social / pension
calculation, payslips, cost reports and an audit trail.

Server-rendered ASP.NET Core 8 MVC with Razor views, EF Core and a local SQLite database
that is created and filled with sample data on first start.

## Tech stack

| Area           | Technology                                                        |
|----------------|-------------------------------------------------------------------|
| Runtime        | .NET 8 / ASP.NET Core 8 MVC, Razor views, custom tag helpers      |
| Data           | Entity Framework Core 8, SQLite, code-first migrations            |
| Auth           | Cookie authentication, role-based authorization, PBKDF2 hashes    |
| UI             | Bootstrap 5.3, jQuery unobtrusive validation (CDN)                |
| Tests          | xUnit                                                             |
| Packaging / CI | Dockerfile, GitHub Actions                                        |

## Prerequisites

* [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.100 or newer), **or**
* Docker 24+ (no .NET SDK needed)

## Running

### With Docker

```bash
docker build -t acme-payroll .
docker run --rm -p 8080:8080 -v payroll-data:/app/App_Data acme-payroll
```

Open <http://localhost:8080>.

### With the .NET SDK

```bash
dotnet run --project src/Payroll.Web
```

Open <http://localhost:5080>.

The SQLite file is created at `src/Payroll.Web/App_Data/payroll.db`; migrations are applied
and sample data is seeded automatically on start. Delete the file to start over with fresh data.
Sample dates are relative to the day the database is created (two paid months plus a draft run
for the current month).

## Demo accounts

All accounts use the password **`Passw0rd!`**

| Email                | Role     | Can do                                                                 |
|----------------------|----------|------------------------------------------------------------------------|
| `admin@acme.test`    | Admin    | Everything: employees, departments, pay runs, settings, audit log      |
| `manager@acme.test`  | Manager  | Engineering manager: approve/reject team timesheets, reports, employee list |
| `employee@acme.test` | Employee | Own profile, own payslips, enter and submit own timesheets (hourly contractor) |

## Screens / routes

| Route                                   | Screen                                                       | Roles            |
|-----------------------------------------|--------------------------------------------------------------|------------------|
| `/account/login`, `/account/logout`     | Sign in / sign out                                           | anonymous        |
| `/`                                     | Dashboard: headcount, last payroll, pending timesheets, next pay date, cost by department | all |
| `/employees`                            | Employee list: search, department/status filter, sorting, paging | Admin, Manager |
| `/employees/create`, `/employees/edit/{id}` | Employee form with validation                            | Admin            |
| `/employees/details/{id}`               | Profile, salary history (add raise), payslips, deactivate    | Admin, Manager, self |
| `/employees/me`                         | Shortcut to own profile                                      | all              |
| `/timesheets`                           | Timesheet list with status filter                            | all (scoped)     |
| `/timesheets/create`, `/timesheets/edit/{id}` | Weekly timesheet (Mon-Sun, 0-24 h per day), save / submit | owner, Admin  |
| `/timesheets/details/{id}`              | Approve / reject with reason                                 | Manager (own dept), Admin |
| `/payruns`                              | Pay run list                                                 | Admin            |
| `/payruns/create`                       | New pay run for a period                                     | Admin            |
| `/payruns/details/{id}`                 | Calculate, review lines, approve, mark paid, CSV export     | Admin            |
| `/payslips/details/{id}`                | Payslip breakdown, `/payslips/print/{id}`, `/payslips/exportcsv/{id}` | Admin, owner |
| `/payslips/mine`                        | My payslips                                                  | all with employee record |
| `/departments`                          | Department CRUD (delete blocked while it has active employees) | Admin          |
| `/reports`                              | Payroll cost by department for a period, CSV export          | Admin, Manager   |
| `/admin/settings`                       | Tax brackets and contribution rates                          | Admin            |
| `/admin/users`                          | User accounts                                                | Admin            |
| `/admin/auditlog`                       | Audit log with filters                                       | Admin            |
| `/health`                               | Liveness probe                                               | anonymous        |

## Payroll rules

* Salaried: monthly gross = annual salary / 12.
* Hourly: rate x regular hours + rate x 1.5 x overtime hours, overtime = hours above 40 in a week
  (only **approved** timesheets whose week starts inside the pay period are paid).
* Income tax (monthly, progressive): 0-1,000 -> 0 %, 1,000-4,000 -> 15 %, above 4,000 -> 25 %.
* Social contribution 5 % of gross; pension 3 % of gross if the employee opted in.
* Other deductions (health insurance, dental, union dues) are fixed monthly amounts per employee.
* Net = gross - tax - social - pension - other; amounts are rounded to cents.

Rates and brackets can be changed under **Admin > Tax & rates**.

Pay run lifecycle: `Draft -> Calculated -> Approved -> Paid`. Draft and calculated runs can be
recalculated or deleted; paid runs are locked.

## Project structure

```
Payroll.sln
├── src/
│   ├── Payroll.Domain/            entities, enums, calculation engine, services, repository interfaces
│   │   ├── Calculation/           PayrollCalculator, TaxTable, OvertimeCalculator
│   │   ├── Services/              Employee/Timesheet/PayRun/Report/... services
│   │   └── Abstractions/          repository + infrastructure interfaces, query objects
│   ├── Payroll.Infrastructure/    EF Core DbContext, configurations, migrations, repositories, seeding
│   └── Payroll.Web/               MVC controllers, view models, Razor views, tag helpers, validation
├── tests/
│   └── Payroll.Tests/             xUnit tests for the calculation engine
├── Dockerfile
└── .github/workflows/ci.yml
```

## Tests

```bash
dotnet test
```

## Database migrations

Migrations live in `src/Payroll.Infrastructure/Migrations`. To add one:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef migrations add <Name> -p src/Payroll.Infrastructure -s src/Payroll.Web
```
