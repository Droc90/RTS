using RTS.Application.Charting;

namespace RTS.Application.Tests.Charting;

public sealed class InitialIndicatorCalculatorTests
{
    [Fact]
    public void Calculate_populates_each_initial_indicator_after_its_warmup_period()
    {
        var start = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var points = Enumerable.Range(0, 80)
            .Select(index => new FinancialChartPoint(start.AddDays(index), 100 + index, 102 + index,
                99 + index, 101 + index, 1_000 + index))
            .ToArray();

        var result = InitialIndicatorCalculator.Calculate(points).ToArray();

        Assert.Null(result[18].BollingerMiddle);
        Assert.NotNull(result[19].BollingerUpper);
        Assert.Null(result[48].Sma50);
        Assert.Equal(125.5m, result[49].Sma50);
        Assert.NotNull(result[33].MacdSignal);
        Assert.Equal(100m, result[14].Rsi);
    }
}
