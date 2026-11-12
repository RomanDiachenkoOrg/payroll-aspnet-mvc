namespace Payroll.Domain.Calculation;

public sealed record TaxBand(decimal LowerBound, decimal? UpperBound, decimal Rate);

/// <summary>
/// Progressive income tax table applied to monthly gross pay.
/// Each band taxes only the part of the income that falls inside it.
/// </summary>
public sealed class TaxTable
{
    private readonly IReadOnlyList<TaxBand> _bands;

    public TaxTable(IEnumerable<TaxBand> bands)
    {
        _bands = bands.OrderBy(b => b.LowerBound).ToList();
        if (_bands.Count == 0)
            throw new ArgumentException("A tax table needs at least one band.", nameof(bands));
    }

    public IReadOnlyList<TaxBand> Bands => _bands;

    public static TaxTable Default { get; } = new(new[]
    {
        new TaxBand(0m, 1000m, 0m),
        new TaxBand(1000m, 4000m, 0.15m),
        new TaxBand(4000m, null, 0.25m)
    });

    public decimal ComputeTax(decimal monthlyGross)
    {
        if (monthlyGross <= 0) return 0m;

        decimal tax = 0m;
        foreach (var band in _bands)
        {
            if (monthlyGross <= band.LowerBound) break;

            var top = band.UpperBound.HasValue ? Math.Min(monthlyGross, band.UpperBound.Value) : monthlyGross;
            var taxable = top - band.LowerBound;
            if (taxable > 0) tax += taxable * band.Rate;
        }

        return Money.Round(tax);
    }
}
