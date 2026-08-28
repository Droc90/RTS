namespace RTS.Application.Charting;

public sealed record IndicatorCalculationSettings(
    int SmaPeriod = 50,
    int BollingerPeriod = 20,
    decimal BollingerDeviations = 2m,
    int MacdFastPeriod = 12,
    int MacdSlowPeriod = 26,
    int MacdSignalPeriod = 9,
    int RsiPeriod = 14);

public static class InitialIndicatorCalculator
{
    public static IReadOnlyCollection<FinancialChartPoint> Calculate(IEnumerable<FinancialChartPoint> source)
        => Calculate(source, new IndicatorCalculationSettings());

    public static IReadOnlyCollection<FinancialChartPoint> Calculate(IEnumerable<FinancialChartPoint> source,
        IndicatorCalculationSettings settings)
    {
        Validate(settings);
        var points = source.OrderBy(point => point.TimestampUtc).ToArray();
        var closes = points.Select(point => point.Close).ToArray();
        var sma50 = Sma(closes, settings.SmaPeriod);
        var middle = Sma(closes, settings.BollingerPeriod);
        var (upper, lower) = Bollinger(closes, middle, settings.BollingerPeriod, settings.BollingerDeviations);
        var macd = Subtract(Ema(closes, settings.MacdFastPeriod), Ema(closes, settings.MacdSlowPeriod));
        var signal = Ema(macd, settings.MacdSignalPeriod);
        var histogram = Subtract(macd, signal);
        var rsi = Rsi(closes, settings.RsiPeriod);

        return points.Select((point, index) => point with
        {
            Sma50 = sma50[index],
            BollingerUpper = upper[index],
            BollingerMiddle = middle[index],
            BollingerLower = lower[index],
            Macd = macd[index],
            MacdSignal = signal[index],
            MacdHistogram = histogram[index],
            Rsi = rsi[index]
        }).ToArray();
    }

    private static void Validate(IndicatorCalculationSettings settings)
    {
        if (settings.SmaPeriod is < 2 or > 250) throw new ArgumentOutOfRangeException(nameof(settings.SmaPeriod));
        if (settings.BollingerPeriod is < 2 or > 250) throw new ArgumentOutOfRangeException(nameof(settings.BollingerPeriod));
        if (settings.BollingerDeviations is < 0.5m or > 5m) throw new ArgumentOutOfRangeException(nameof(settings.BollingerDeviations));
        if (settings.MacdFastPeriod is < 2 or > 100 || settings.MacdSlowPeriod is < 3 or > 250 ||
            settings.MacdFastPeriod >= settings.MacdSlowPeriod) throw new ArgumentException("MACD requires a fast period shorter than the slow period.");
        if (settings.MacdSignalPeriod is < 2 or > 100) throw new ArgumentOutOfRangeException(nameof(settings.MacdSignalPeriod));
        if (settings.RsiPeriod is < 2 or > 100) throw new ArgumentOutOfRangeException(nameof(settings.RsiPeriod));
    }

    private static decimal?[] Sma(decimal[] values, int period)
    {
        var result = new decimal?[values.Length];
        decimal sum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            sum += values[index];
            if (index >= period) sum -= values[index - period];
            if (index >= period - 1) result[index] = sum / period;
        }
        return result;
    }

    private static (decimal?[] Upper, decimal?[] Lower) Bollinger(decimal[] values, decimal?[] middle, int period, decimal deviations)
    {
        var upper = new decimal?[values.Length];
        var lower = new decimal?[values.Length];
        for (var index = period - 1; index < values.Length; index++)
        {
            var mean = middle[index]!.Value;
            var variance = values.Skip(index - period + 1).Take(period).Sum(value => (value - mean) * (value - mean)) / period;
            var deviation = (decimal)Math.Sqrt((double)variance) * deviations;
            upper[index] = mean + deviation;
            lower[index] = mean - deviation;
        }
        return (upper, lower);
    }

    private static decimal?[] Ema(decimal[] values, int period) => Ema(values.Select(value => (decimal?)value).ToArray(), period);

    private static decimal?[] Ema(decimal?[] values, int period)
    {
        var result = new decimal?[values.Length];
        var available = values.Select((value, index) => (value, index)).Where(item => item.value.HasValue).ToArray();
        if (available.Length < period) return result;
        var seedIndex = available[period - 1].index;
        var previous = available.Take(period).Average(item => item.value!.Value);
        result[seedIndex] = previous;
        var multiplier = 2m / (period + 1m);
        foreach (var item in available.Skip(period))
        {
            previous = (item.value!.Value - previous) * multiplier + previous;
            result[item.index] = previous;
        }
        return result;
    }

    private static decimal?[] Subtract(decimal?[] left, decimal?[] right) =>
        left.Select((value, index) => value.HasValue && right[index].HasValue ? value - right[index] : null).ToArray();

    private static decimal?[] Rsi(decimal[] values, int period)
    {
        var result = new decimal?[values.Length];
        if (values.Length <= period) return result;
        decimal gains = 0, losses = 0;
        for (var index = 1; index <= period; index++)
        {
            var change = values[index] - values[index - 1];
            if (change >= 0) gains += change; else losses -= change;
        }
        var averageGain = gains / period;
        var averageLoss = losses / period;
        result[period] = RsiValue(averageGain, averageLoss);
        for (var index = period + 1; index < values.Length; index++)
        {
            var change = values[index] - values[index - 1];
            averageGain = (averageGain * (period - 1) + Math.Max(change, 0)) / period;
            averageLoss = (averageLoss * (period - 1) + Math.Max(-change, 0)) / period;
            result[index] = RsiValue(averageGain, averageLoss);
        }
        return result;
    }

    private static decimal RsiValue(decimal gain, decimal loss) => loss == 0 ? 100m : 100m - 100m / (1m + gain / loss);
}
