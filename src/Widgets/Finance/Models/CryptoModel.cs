namespace Finance.Models;

internal static class CurrencyFormatting
{
    public static string GetSymbolOrPrefix(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "USD" or "CAD" or "AUD" or "SGD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "JPY" or "CNY" => "¥",
        "PHP" => "₱",
        "INR" => "₹",
        "KRW" => "₩",
        "CHF" => "CHF ",
        null or "" => "$",
        var other => $"{other} "
    };

    public static string FormatPrice(double value, string? currencyCode, int decimalsWhenMedium = 2)
    {
        var prefix = GetSymbolOrPrefix(currencyCode);
        return value switch
        {
            >= 1000 => $"{prefix}{value:N0}",
            >= 1 => $"{prefix}{value.ToString($"N{decimalsWhenMedium}")}",
            _ => $"{prefix}{value:N4}"
        };
    }
}

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
    List<double> SparklinePrices,
    string FiatCurrency = "usd")
{
    public bool IsPositive => PriceChangePercentage24h >= 0;

    public string FormattedPrice => CurrencyFormatting.FormatPrice(CurrentPrice, FiatCurrency);

    public string FormattedChange => $"{(IsPositive ? "+" : "")}{PriceChangePercentage24h:F2}%";
}
