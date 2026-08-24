namespace RTS.Infrastructure.MarketData;

public sealed class AlpacaMarketDataOptions
{
    public const string SectionName = "MarketData:Alpaca";
    public string ApiKeyId { get; set; } = string.Empty;
    public string ApiSecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://data.alpaca.markets";
    public string Feed { get; set; } = "iex";
    public int TimeoutSeconds { get; set; } = 60;
}
