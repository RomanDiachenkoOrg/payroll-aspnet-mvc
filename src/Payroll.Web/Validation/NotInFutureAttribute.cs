using System.ComponentModel.DataAnnotations;

namespace Payroll.Web.Validation;

/// <summary>Validates that a date is today or earlier.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    public NotInFutureAttribute() : base("{0} cannot be in the future.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true; // [Required] handles missing values

        var date = value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            _ => throw new InvalidOperationException($"{nameof(NotInFutureAttribute)} only supports dates.")
        };

        return date.Date <= DateTime.Today.AddDays(1);
    }
}
