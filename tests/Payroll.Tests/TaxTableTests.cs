using Payroll.Domain.Calculation;

namespace Payroll.Tests;

public class TaxTableTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(500, 0)]
    [InlineData(1000, 0)]
    [InlineData(1500, 75)]
    [InlineData(4000, 450)]
    [InlineData(5000, 700)]
    [InlineData(10000, 1950)]
    public void Default_table_is_progressive(decimal gross, decimal expectedTax)
    {
        Assert.Equal(expectedTax, TaxTable.Default.ComputeTax(gross));
    }

    [Fact]
    public void Negative_income_has_no_tax()
    {
        Assert.Equal(0m, TaxTable.Default.ComputeTax(-100m));
    }

    [Fact]
    public void Bands_are_sorted_by_lower_bound()
    {
        var table = new TaxTable(new[]
        {
            new TaxBand(2000m, null, 0.3m),
            new TaxBand(0m, 2000m, 0.1m)
        });

        Assert.Equal(0m, table.Bands[0].LowerBound);
        Assert.Equal(200m + 300m, table.ComputeTax(3000m));
    }

    [Fact]
    public void Empty_table_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TaxTable(Array.Empty<TaxBand>()));
    }
}
