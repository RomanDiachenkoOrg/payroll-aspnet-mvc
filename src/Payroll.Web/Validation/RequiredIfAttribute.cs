using System.ComponentModel.DataAnnotations;

namespace Payroll.Web.Validation;

/// <summary>
/// Makes a property required (and > 0 for numbers) when another property has a given value,
/// e.g. AnnualSalary is required when PayType == Salaried.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredPositiveIfAttribute : ValidationAttribute
{
    public RequiredPositiveIfAttribute(string otherProperty, object expectedValue)
        : base("{0} must be greater than zero.")
    {
        OtherProperty = otherProperty;
        ExpectedValue = expectedValue;
    }

    public string OtherProperty { get; }
    public object ExpectedValue { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var other = context.ObjectType.GetProperty(OtherProperty)
                    ?? throw new InvalidOperationException($"Unknown property {OtherProperty}.");
        var otherValue = other.GetValue(context.ObjectInstance);

        if (!Equals(otherValue, ExpectedValue)) return ValidationResult.Success;

        var ok = value switch
        {
            null => false,
            decimal d => d > 0,
            int i => i > 0,
            double db => db > 0,
            _ => true
        };

        return ok
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(context.DisplayName), new[] { context.MemberName! });
    }
}
