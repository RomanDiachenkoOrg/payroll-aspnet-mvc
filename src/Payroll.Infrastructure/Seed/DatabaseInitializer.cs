using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payroll.Domain.Abstractions;
using Payroll.Domain.Calculation;
using Payroll.Infrastructure.Data;

namespace Payroll.Infrastructure.Seed;

public class DatabaseInitializer
{
    private readonly PayrollDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPayrollCalculator _calculator;
    private readonly IClock _clock;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        PayrollDbContext db,
        IPasswordHasher hasher,
        IPayrollCalculator calculator,
        IClock clock,
        ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _hasher = hasher;
        _calculator = calculator;
        _clock = clock;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);

        if (await _db.Departments.AnyAsync(ct))
        {
            _logger.LogInformation("Database already contains data, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding sample data");
        var seeder = new SampleDataSeeder(_db, _hasher, _calculator, _clock.Today);
        await seeder.SeedAsync(ct);
        _logger.LogInformation("Sample data created");
    }
}
