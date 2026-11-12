using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Data.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.RejectionReason).HasMaxLength(500);
        builder.HasIndex(t => new { t.EmployeeId, t.WeekStart }).IsUnique();
        builder.HasIndex(t => t.Status);

        builder.HasOne(t => t.Employee)
            .WithMany(e => e.Timesheets)
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Entries)
            .WithOne(e => e.Timesheet)
            .HasForeignKey(e => e.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.TotalHours);
        builder.Ignore(t => t.WeekEnd);
        builder.Ignore(t => t.IsEditable);
    }
}

public class PayRunConfiguration : IEntityTypeConfiguration<PayRun>
{
    public void Configure(EntityTypeBuilder<PayRun> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => p.PeriodStart);

        builder.HasMany(p => p.Payslips)
            .WithOne(s => s.PayRun)
            .HasForeignKey(s => s.PayRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.IsLocked);
        builder.Ignore(p => p.TotalGross);
        builder.Ignore(p => p.TotalNet);
        builder.Ignore(p => p.TotalDeductions);
    }
}

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.Property(p => p.PayType).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => new { p.PayRunId, p.EmployeeId }).IsUnique();
        builder.HasOne(p => p.Employee)
            .WithMany(e => e.Payslips)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(p => p.TotalDeductions);
    }
}

public class TaxBracketConfiguration : IEntityTypeConfiguration<TaxBracket>
{
    public void Configure(EntityTypeBuilder<TaxBracket> builder)
    {
        builder.Property(t => t.Rate).HasPrecision(5, 4);
    }
}

public class PayrollSettingsConfiguration : IEntityTypeConfiguration<PayrollSettings>
{
    public void Configure(EntityTypeBuilder<PayrollSettings> builder)
    {
        builder.Property(s => s.SocialContributionRate).HasPrecision(5, 4);
        builder.Property(s => s.PensionRate).HasPrecision(5, 4);
        builder.Property(s => s.Currency).HasMaxLength(3);
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(300).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasOne(u => u.Employee).WithMany().HasForeignKey(u => u.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(u => u.Department).WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.UserEmail).HasMaxLength(150);
        builder.Property(a => a.Action).HasMaxLength(50);
        builder.Property(a => a.EntityType).HasMaxLength(50);
        builder.Property(a => a.EntityId).HasMaxLength(50);
        builder.Property(a => a.Details).HasMaxLength(500);
        builder.HasIndex(a => a.Timestamp);
    }
}
