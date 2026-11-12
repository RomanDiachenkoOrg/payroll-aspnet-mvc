using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payroll.Domain.Entities;

namespace Payroll.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(10).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.CostCenter).HasMaxLength(20);
        builder.HasIndex(d => d.Name).IsUnique();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(e => e.EmployeeNumber).HasMaxLength(10).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(60).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(60).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.JobTitle).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PayType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.EmployeeNumber).IsUnique();
        builder.HasIndex(e => new { e.DepartmentId, e.IsActive });

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.FullName);
        builder.Ignore(e => e.MonthlyOtherDeductions);
    }
}

public class SalaryHistoryConfiguration : IEntityTypeConfiguration<SalaryHistory>
{
    public void Configure(EntityTypeBuilder<SalaryHistory> builder)
    {
        builder.ToTable("SalaryHistory");
        builder.Property(s => s.PayType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Reason).HasMaxLength(200);
        builder.HasOne(s => s.Employee)
            .WithMany(e => e.SalaryHistory)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeductionTypeConfiguration : IEntityTypeConfiguration<DeductionType>
{
    public void Configure(EntityTypeBuilder<DeductionType> builder)
    {
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();
    }
}

public class EmployeeDeductionConfiguration : IEntityTypeConfiguration<EmployeeDeduction>
{
    public void Configure(EntityTypeBuilder<EmployeeDeduction> builder)
    {
        builder.HasOne(d => d.Employee)
            .WithMany(e => e.Deductions)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(d => d.DeductionType)
            .WithMany()
            .HasForeignKey(d => d.DeductionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => new { d.EmployeeId, d.DeductionTypeId }).IsUnique();
    }
}
