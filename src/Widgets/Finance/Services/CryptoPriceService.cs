using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Media;
using Finance.Models;
using uWidgets.Core;

namespace Finance.Services;

public record CryptoPricesResult(List<CryptoCoinInfo> Coins, bool IsOfflineFallback);

public static class CryptoPriceService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly string CachePath = Path.Combine(Const.CurrentFolder, "finance_crypto_cache.json");
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    private record CachedCryptoPayload(string Fiat, List<CryptoCoinInfo> Coins);

    static CryptoPriceService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("uWidgets/1.0 (Windows; OpenSource)");
    }

    public static async Task<List<CryptoCoinInfo>> GetCryptoPricesAsync(List<string> coinIds, string fiat = "usd") =>
        (await GetCryptoPricesWithStatusAsync(coinIds, fiat)).Coins;

    public static async Task<CryptoPricesResult> GetCryptoPricesWithStatusAsync(List<string> coinIds, string fiat = "usd")
    {
        if (coinIds.Count == 0) return new CryptoPricesResult([], false);

        var normalizedFiat = string.IsNullOrWhiteSpace(fiat) ? "usd" : fiat.Trim().ToLowerInvariant();
        var cleanIds = coinIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (cleanIds.Count == 0) return new CryptoPricesResult([], false);

        var idsParam = string.Join(",", cleanIds.Select(Uri.EscapeDataString));
        var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency={Uri.EscapeDataString(normalizedFiat)}&ids={idsParam}&sparkline=true&price_change_percentage=24h";

        try
        {
            using var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = ParseCoinGeckoJson(json, normalizedFiat);
                if (result.Count > 0)
                {
                    await WriteCacheSafeAsync(normalizedFiat, result);
                }
                return new CryptoPricesResult(result, false);
            }
        }
        catch
        {
            // Offline or rate limited - fall back to disk cache
        }

        return LoadFromDiskCache(cleanIds, normalizedFiat);
    }

    private static async Task WriteCacheSafeAsync(string fiat, List<CryptoCoinInfo> coins)
    {
        await CacheLock.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(CachePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new CachedCryptoPayload(fiat, coins));
            await File.WriteAllTextAsync(CachePath, json);
        }
        catch
        {
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private static List<CryptoCoinInfo> ParseCoinGeckoJson(string json, string fiat)
    {
        var list = new List<CryptoCoinInfo>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return list;

        foreach (var elem in doc.RootElement.EnumerateArray())
        {
            var id = elem.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
            var symbol = elem.TryGetProperty("symbol", out var symProp) ? symProp.GetString()?.ToUpperInvariant() ?? "" : "";
            var name = elem.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(id) ||
                !elem.TryGetProperty("current_price", out var priceProp) ||
                priceProp.ValueKind != JsonValueKind.Number ||
                !priceProp.TryGetDouble(out var price))
            {
                continue;
            }

            var change24h = elem.TryGetProperty("price_change_percentage_24h", out var ch) &&
                            ch.ValueKind == JsonValueKind.Number &&
                            ch.TryGetDouble(out var chVal)
                ? chVal
                : 0.0;

            var sparkline = new List<double>();
            if (elem.TryGetProperty("sparkline_in_7d", out var sp) &&
                sp.ValueKind == JsonValueKind.Object &&
                sp.TryGetProperty("price", out var prices) &&
                prices.ValueKind == JsonValueKind.Array)
            {
                // Last 24 entries represent approximately the last 24 hours of hourly data
                sparkline = prices.EnumerateArray()
                    .Where(p => p.ValueKind == JsonValueKind.Number)
                    .Select(p => p.GetDouble())
                    .TakeLast(24)
                    .ToList();
            }

            list.Add(new CryptoCoinInfo(id, symbol, name, price, change24h, sparkline, fiat));
        }

        return list;
    }

    private static CryptoPricesResult LoadFromDiskCache(List<string> requestedIds, string fiat)
    {
        try
        {
            if (File.Exists(CachePath))
            {
                var json = File.ReadAllText(CachePath);
                if (json.TrimStart().StartsWith("[", StringComparison.Ordinal))
                {
                    var legacy = ParseCoinGeckoJson(json, "usd");
                    if (string.Equals(fiat, "usd", StringComparison.OrdinalIgnoreCase))
                    {
                        var filtered = legacy.Where(c => requestedIds.Contains(c.Id, StringComparer.OrdinalIgnoreCase)).ToList();
                        if (filtered.Count > 0) return new CryptoPricesResult(filtered, true);
                    }
                }
                else
                {
                    var cached = JsonSerializer.Deserialize<CachedCryptoPayload>(json);
                    if (cached != null && string.Equals(cached.Fiat, fiat, StringComparison.OrdinalIgnoreCase))
                    {
                        var filtered = cached.Coins
                            .Where(c => requestedIds.Contains(c.Id, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                        if (filtered.Count > 0) return new CryptoPricesResult(filtered, true);
                    }
                }
            }
        }
        catch { }

        // Minimal seed data only for default coins when offline in USD
        if (string.Equals(fiat, "usd", StringComparison.OrdinalIgnoreCase))
        {
            CryptoCoinInfo[] seeds =
            [
                new CryptoCoinInfo("bitcoin", "BTC", "Bitcoin", 89400.0, 3.45, [86000, 87200, 88100, 89400], "usd"),
                new CryptoCoinInfo("ethereum", "ETH", "Ethereum", 3120.0, 1.82, [3050, 3080, 3100, 3120], "usd"),
                new CryptoCoinInfo("solana", "SOL", "Solana", 214.50, 5.21, [198, 204, 210, 214.50], "usd")
            ];
            var matched = seeds.Where(s => requestedIds.Contains(s.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            if (matched.Count > 0) return new CryptoPricesResult(matched, true);
        }

        return new CryptoPricesResult([], true);
    }

    public static StreamGeometry GenerateSparklineGeometry(List<double> prices, double width = 60, double height = 20)
    {
        var geometry = new StreamGeometry();
        if (prices.Count < 2) return geometry;

        double min = prices.Min();
        double max = prices.Max();
        double range = max - min;
        if (range <= 0.00001) range = 1.0;

        using var ctx = geometry.Open();

        double stepX = width / (prices.Count - 1);
        double firstY = height - ((prices[0] - min) / range) * (height - 4) - 2;

        ctx.BeginFigure(new Point(0, firstY), false);

        for (int i = 1; i < prices.Count; i++)
        {
            double x = i * stepX;
            double y = height - ((prices[i] - min) / range) * (height - 4) - 2;
            ctx.LineTo(new Point(x, y));
        }

        return geometry;
    }
}
