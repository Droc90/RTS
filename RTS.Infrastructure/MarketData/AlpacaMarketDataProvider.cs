using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RTS.Application.MarketData;
using RTS.Domain.CandidateDiscovery;
using RTS.Domain.TradingModels.Timeframes;

namespace RTS.Infrastructure.MarketData;

public sealed class AlpacaMarketDataProvider(IOptions<AlpacaMarketDataOptions> configured) : IMarketPriceDataProvider
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    private static readonly HttpClient Client = new();
    private readonly AlpacaMarketDataOptions options = configured.Value;
    public string ProviderKey => $"alpaca:{options.Feed}";
    public MarketDataCoveragePolicy Coverage { get; } = new(new HashSet<string>(["NYSE", "NASDAQ", "NYSE American"], StringComparer.OrdinalIgnoreCase), new HashSet<string>(["USD"]), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(20), true, "Market data is supplied by Alpaca and remains subject to the user's Alpaca subscription and display rights.");

    public async Task<IReadOnlyCollection<PriceBar>> GetBarsAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        Validate();
        var results = new List<PriceBar>();
        string? pageToken = null;
        do
        {
            var path = $"/v2/stocks/{Uri.EscapeDataString(CandidateUniverse.NormalizeSymbol(request.Symbol))}/bars?start={Encode(request.FromUtc.ToString("O"))}&end={Encode(request.ToUtc.ToString("O"))}&timeframe={Encode(Timeframe(request))}&adjustment={(request.AdjustForCorporateActions ? "all" : "raw")}&feed={Encode(options.Feed)}&limit=10000";
            if (!string.IsNullOrWhiteSpace(pageToken)) path += $"&page_token={Encode(pageToken)}";
            using var document = await GetJsonAsync(path, cancellationToken);
            if (document.RootElement.TryGetProperty("bars", out var bars))
                foreach (var bar in bars.EnumerateArray())
                {
                    var timestamp = bar.GetProperty("t").GetDateTime().ToUniversalTime();
                    results.Add(new PriceBar(timestamp, bar.GetProperty("o").GetDecimal(), bar.GetProperty("h").GetDecimal(), bar.GetProperty("l").GetDecimal(), bar.GetProperty("c").GetDecimal(), bar.GetProperty("v").GetDecimal(), IsExtendedHours(timestamp, request.IntervalUnit), request.AdjustForCorporateActions));
                }
            pageToken = document.RootElement.TryGetProperty("next_page_token", out var token) && token.ValueKind == JsonValueKind.String ? token.GetString() : null;
        } while (!string.IsNullOrWhiteSpace(pageToken));
        if (results.Count == 0) throw new InvalidOperationException($"Alpaca returned no bars for {request.Symbol}.");
        return results;
    }

    public async Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Validate();
        symbol = CandidateUniverse.NormalizeSymbol(symbol);
        using var document = await GetJsonAsync($"/v2/stocks/{Uri.EscapeDataString(symbol)}/snapshot?feed={Encode(options.Feed)}", cancellationToken);
        var quote = document.RootElement.GetProperty("latestQuote");
        var trade = document.RootElement.GetProperty("latestTrade");
        var last = trade.GetProperty("p").GetDecimal();
        decimal? previousClose = document.RootElement.TryGetProperty("prevDailyBar", out var previousBar) &&
            previousBar.ValueKind == JsonValueKind.Object && previousBar.TryGetProperty("c", out var close)
                ? close.GetDecimal()
                : null;
        decimal? changePercent = previousClose is > 0 ? (last - previousClose.Value) / previousClose.Value * 100m : null;
        return new MarketQuote(symbol, quote.GetProperty("bp").GetDecimal(), quote.GetProperty("ap").GetDecimal(),
            last, quote.GetProperty("t").GetDateTime().ToUniversalTime(), previousClose, changePercent);
    }

    public async Task<IReadOnlyCollection<CorporateAction>> GetCorporateActionsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        Validate();
        symbol = CandidateUniverse.NormalizeSymbol(symbol);
        var path = $"/v1/corporate-actions?symbols={Encode(symbol)}&start={fromUtc:yyyy-MM-dd}&end={toUtc:yyyy-MM-dd}&sort=asc&limit=1000&data_quality=complete";
        using var document = await GetJsonAsync(path, cancellationToken);
        var results = new List<CorporateAction>();
        ReadActions(document.RootElement, "forward_splits", CorporateActionType.Split, symbol, results);
        ReadActions(document.RootElement, "reverse_splits", CorporateActionType.Split, symbol, results);
        ReadActions(document.RootElement, "cash_dividends", CorporateActionType.CashDividend, symbol, results);
        ReadActions(document.RootElement, "name_changes", CorporateActionType.SymbolChange, symbol, results);
        return results.OrderBy(item => item.EffectiveUtc).ToArray();
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(options.BaseUrl.TrimEnd('/') + "/"), path.TrimStart('/')));
        request.Headers.Add("APCA-API-KEY-ID", options.ApiKeyId);
        request.Headers.Add("APCA-API-SECRET-KEY", options.ApiSecretKey);
        using var response = await Client.SendAsync(request, timeout.Token);
        var content = await response.Content.ReadAsStringAsync(timeout.Token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Alpaca market data failed ({(int)response.StatusCode}): {ReadMessage(content)}");
        return JsonDocument.Parse(content);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(options.ApiKeyId) || string.IsNullOrWhiteSpace(options.ApiSecretKey)) throw new InvalidOperationException("Alpaca market data is not configured. Set MarketData:Alpaca:ApiKeyId and ApiSecretKey using user secrets or environment variables.");
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("The Alpaca base URL must be an absolute HTTPS URL.");
        if (options.TimeoutSeconds is < 10 or > 300) throw new InvalidOperationException("The Alpaca timeout must be between 10 and 300 seconds.");
    }

    private static void ReadActions(JsonElement root, string property, CorporateActionType type, string symbol, ICollection<CorporateAction> results)
    {
        if (!root.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array) return;
        foreach (var value in values.EnumerateArray())
        {
            var dateText = GetString(value, "ex_date") ?? GetString(value, "process_date") ?? GetString(value, "payable_date");
            if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var effectiveUtc)) continue;
            decimal? factor = Decimal(value, "new_rate");
            var oldRate = Decimal(value, "old_rate");
            if (factor is not null && oldRate is > 0) factor /= oldRate;
            results.Add(new CorporateAction(symbol, type, DateTime.SpecifyKind(effectiveUtc, DateTimeKind.Utc), factor, Decimal(value, "rate") ?? Decimal(value, "cash"), GetString(value, "new_symbol")));
        }
    }

    private static string Timeframe(MarketDataRequest request) => request.IntervalUnit switch { BarIntervalUnit.Minutes => $"{request.IntervalValue}Min", BarIntervalUnit.Hours => $"{request.IntervalValue}Hour", BarIntervalUnit.Days => $"{request.IntervalValue}Day", BarIntervalUnit.Weeks => $"{request.IntervalValue}Week", _ => throw new NotSupportedException("Alpaca does not support the requested bar interval.") };
    internal static bool IsExtendedHours(DateTime utc, BarIntervalUnit intervalUnit)
    {
        // Alpaca stamps daily and weekly aggregate bars at midnight UTC. That timestamp
        // identifies the trading date; it is not an extended-hours trade timestamp.
        if (intervalUnit is BarIntervalUnit.Days or BarIntervalUnit.Weeks) return false;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Eastern);
        var time = local.TimeOfDay;
        return local.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
            time < TimeSpan.FromHours(9.5) || time >= TimeSpan.FromHours(16);
    }
    private static string Encode(string value) => Uri.EscapeDataString(value);
    private static string? GetString(JsonElement value, string property) => value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    private static decimal? Decimal(JsonElement value, string property) => value.TryGetProperty(property, out var item) && (item.ValueKind == JsonValueKind.Number ? item.TryGetDecimal(out var number) : decimal.TryParse(item.GetString(), CultureInfo.InvariantCulture, out number)) ? number : null;
    private static string ReadMessage(string content) { try { using var json = JsonDocument.Parse(content); return json.RootElement.TryGetProperty("message", out var value) ? value.GetString() ?? "Unknown error." : "Unknown error."; } catch (JsonException) { return "The provider returned an unreadable error."; } }
}
