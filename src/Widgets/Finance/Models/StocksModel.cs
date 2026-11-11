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
    double PercentChange)
{
    public bool IsPositive => Change >= 0;

    public string FormattedPrice => CurrentPrice switch
    {
        >= 1000 => $"{CurrentPrice:N2}",
        _ => $"{CurrentPrice:N2}"
    };

    public string FormattedChange => $"{(IsPositive ? "+" : "")}{PercentChange:F2}%";
    public string FormattedDelta => $"{(IsPositive ? "+" : "")}{Change:F2}";
}
