using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payroll.Infrastructure.Data;

/// <summary>Used by <c>dotnet ef</c> when creating migrations.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PayrollDbContext>
{
    public PayrollDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite("Data Source=payroll-design.db")
            .Options;
        return new PayrollDbContext(options);
    }
}
