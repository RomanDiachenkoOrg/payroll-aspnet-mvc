using Payroll.Domain.Abstractions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;

namespace Payroll.Domain.Services;

public interface ISettingsService
{
    Task<PayrollSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaxBracket>> GetTaxBracketsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DeductionType>> GetDeductionTypesAsync(CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(PayrollSettings values, IReadOnlyList<TaxBracket> brackets, CancellationToken ct = default);
}

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SettingsService(ISettingsRepository repository, IUnitOfWork uow, IAuditService audit, ICurrentUser user, IClock clock)
    {
        _repository = repository;
        _uow = uow;
        _audit = audit;
        _user = user;
        _clock = clock;
    }

    public Task<PayrollSettings> GetSettingsAsync(CancellationToken ct = default) => _repository.GetSettingsAsync(ct);

    public Task<IReadOnlyList<TaxBracket>> GetTaxBracketsAsync(CancellationToken ct = default) => _repository.GetTaxBracketsAsync(ct);

    public Task<IReadOnlyList<DeductionType>> GetDeductionTypesAsync(CancellationToken ct = default) => _repository.GetDeductionTypesAsync(ct);

    public async Task<OperationResult> UpdateAsync(PayrollSettings values, IReadOnlyList<TaxBracket> brackets, CancellationToken ct = default)
    {
        var errors = ValidateBrackets(brackets).ToList();
        if (values.SocialContributionRate is < 0 or > 1) errors.Add(new("SocialContributionPercent", "Rate must be between 0 and 100%."));
        if (values.PensionRate is < 0 or > 1) errors.Add(new("PensionPercent", "Rate must be between 0 and 100%."));
        if (values.PayDayOfMonth is < 1 or > 31) errors.Add(new("PayDayOfMonth", "Pay day must be between 1 and 31."));
        if (errors.Count > 0) return OperationResult.Failure(errors);

        var settings = await _repository.GetSettingsAsync(ct);
        settings.SocialContributionRate = values.SocialContributionRate;
        settings.PensionRate = values.PensionRate;
        settings.OvertimeMultiplier = values.OvertimeMultiplier;
        settings.WeeklyOvertimeThreshold = values.WeeklyOvertimeThreshold;
        settings.PayDayOfMonth = values.PayDayOfMonth;
        settings.UpdatedAt = _clock.UtcNow;
        settings.UpdatedBy = _user.Email;

        var ordered = brackets.OrderBy(b => b.LowerBound).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i + 1;
        _repository.ReplaceTaxBrackets(ordered);

        _audit.Record(AuditActions.SettingsChange, nameof(PayrollSettings), settings.Id,
            "Brackets: " + string.Join("; ", ordered.Select(b => $"{b.LowerBound:0.##}-{(b.UpperBound?.ToString("0.##") ?? "max")} @ {b.Rate:P0}")));
        await _uow.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private static IEnumerable<OperationError> ValidateBrackets(IReadOnlyList<TaxBracket> brackets)
    {
        if (brackets.Count == 0)
        {
            yield return new OperationError(null, "At least one tax bracket is required.");
            yield break;
        }

        var ordered = brackets.OrderBy(b => b.LowerBound).ToList();
        if (ordered[0].LowerBound != 0)
            yield return new OperationError(null, "The first tax bracket must start at 0.");

        for (var i = 0; i < ordered.Count; i++)
        {
            var b = ordered[i];
            if (b.Rate is < 0 or > 1)
                yield return new OperationError(null, $"Bracket {i + 1}: rate must be between 0 and 100%.");

            var isLast = i == ordered.Count - 1;
            if (!isLast)
            {
                if (b.UpperBound is null)
                    yield return new OperationError(null, $"Bracket {i + 1}: only the last bracket can be open-ended.");
                else if (b.UpperBound != ordered[i + 1].LowerBound)
                    yield return new OperationError(null, $"Bracket {i + 1}: upper bound must equal the next bracket's lower bound.");
            }
            else if (b.UpperBound is not null)
            {
                yield return new OperationError(null, "The last bracket must not have an upper bound.");
            }
        }
    }
}
