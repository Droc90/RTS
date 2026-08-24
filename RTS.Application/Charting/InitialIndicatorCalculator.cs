namespace RTS.Application.Charting;

public static class InitialIndicatorCalculator
{
    public static IReadOnlyCollection<FinancialChartPoint> Calculate(IEnumerable<FinancialChartPoint> source)
    {
        var points = source.OrderBy(point => point.TimestampUtc).ToArray();
        var closes = points.Select(point => point.Close).ToArray();
        var sma50 = Sma(closes, 50);
        var middle = Sma(closes, 20);
        var (upper, lower) = Bollinger(closes, middle, 20, 2m);
        var macd = Subtract(Ema(closes, 12), Ema(closes, 26));
        var signal = Ema(macd, 9);
        var histogram = Subtract(macd, signal);
        var rsi = Rsi(closes, 14);

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
