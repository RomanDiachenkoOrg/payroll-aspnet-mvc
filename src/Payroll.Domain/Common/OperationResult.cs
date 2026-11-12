namespace Payroll.Domain.Common;

public class OperationResult
{
    protected OperationResult(bool succeeded, IReadOnlyList<OperationError> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public IReadOnlyList<OperationError> Errors { get; }
    public string ErrorMessage => string.Join(" ", Errors.Select(e => e.Message));

    public static OperationResult Success() => new(true, Array.Empty<OperationError>());

    public static OperationResult Failure(string message, string? field = null) =>
        new(false, new[] { new OperationError(field, message) });

    public static OperationResult Failure(IEnumerable<OperationError> errors) => new(false, errors.ToList());
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool succeeded, T? value, IReadOnlyList<OperationError> errors)
        : base(succeeded, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(true, value, Array.Empty<OperationError>());

    public new static OperationResult<T> Failure(string message, string? field = null) =>
        new(false, default, new[] { new OperationError(field, message) });

    public new static OperationResult<T> Failure(IEnumerable<OperationError> errors) =>
        new(false, default, errors.ToList());
}

/// <param name="Field">Name of the model property the error belongs to, or null for a general error.</param>
public sealed record OperationError(string? Field, string Message);
