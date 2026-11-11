namespace Finance.Models;

public record CryptoModel
{
    public List<string> TrackedCoins { get; init; } = ["bitcoin", "ethereum", "solana"];
    public string FiatCurrency { get; init; } = "usd";
    public int RefreshMinutes { get; init; } = 1;
}

public record CryptoCoinInfo(
    string Id,
    string Symbol,
    string Name,
    double CurrentPrice,
    double PriceChangePercentage24h,
    List<double> SparklinePrices)
{
    public bool IsPositive => PriceChangePercentage24h >= 0;

    public string FormattedPrice => CurrentPrice switch
    {
        >= 1000 => $"${CurrentPrice:N0}",
        >= 1 => $"${CurrentPrice:N2}",
        _ => $"${CurrentPrice:N4}"
    };

    public string FormattedChange => $"{(IsPositive ? "+" : "")}{PriceChangePercentage24h:F2}%";
}
