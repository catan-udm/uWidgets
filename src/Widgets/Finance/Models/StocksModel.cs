namespace Finance.Models;

public record StocksModel
{
    public List<string> TrackedTickers { get; init; } = ["^GSPC", "^IXIC", "NVDA", "AAPL", "MSFT"];
    public int RefreshMinutes { get; init; } = 1;
}

public record StockQuoteInfo(
    string Symbol,
    string DisplayName,
    double CurrentPrice,
    double Change,
    double PercentChange,
    string Currency = "USD")
{
    public bool IsPositive => Change >= 0;

    public string FormattedPrice => $"{CurrencyFormatting.GetSymbolOrPrefix(Currency)}{CurrentPrice:N2}";

    public string FormattedChange => $"{(IsPositive ? "+" : "")}{PercentChange:F2}%";
    public string FormattedDelta => $"{(IsPositive ? "+" : "")}{Change:F2}";
}
