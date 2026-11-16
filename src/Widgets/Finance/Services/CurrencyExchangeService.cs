using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using uWidgets.Core;

namespace Finance.Services;

public record ExchangeRatesResult(
    Dictionary<string, double> Rates,
    bool IsOfflineFallback,
    DateTimeOffset? UpdatedAtUtc);

public static class CurrencyExchangeService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly string CachePath = Path.Combine(Const.CurrentFolder, "finance_exchange_rates.json");
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    private static ExchangeRatesResult? cachedResult;

    public static async Task<ExchangeRatesResult> GetExchangeRatesWithStatusAsync(bool forceRefresh = false)
    {
        var current = cachedResult;
        if (!forceRefresh &&
            current is { Rates.Count: > 0, IsOfflineFallback: false } &&
            current.UpdatedAtUtc.HasValue &&
            DateTimeOffset.UtcNow - current.UpdatedAtUtc.Value < TimeSpan.FromMinutes(30))
        {
            return current;
        }

        try
        {
            using var response = await HttpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var rates = ParseRates(json);
                if (rates.Count > 0)
                {
                    var live = new ExchangeRatesResult(rates, false, DateTimeOffset.UtcNow);
                    cachedResult = live;
                    await WriteCacheSafeAsync(json);
                    return live;
                }
            }
        }
        catch
        {
            // Offline - retry on subsequent refresh
        }

        if (current is { Rates.Count: > 0 })
            return current with { IsOfflineFallback = true };

        var fallback = LoadFromDiskCache();
        cachedResult = fallback;
        return fallback;
    }

    public static async Task<Dictionary<string, double>> GetExchangeRatesAsync(bool forceRefresh = false) =>
        (await GetExchangeRatesWithStatusAsync(forceRefresh)).Rates;

    private static async Task WriteCacheSafeAsync(string json)
    {
        await CacheLock.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(CachePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
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

    private static Dictionary<string, double> ParseRates(string json)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("rates", out var ratesProp) && ratesProp.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in ratesProp.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDouble(out var rate) && rate > 0)
                {
                    dict[prop.Name] = rate;
                }
            }
        }
        return dict;
    }

    private static ExchangeRatesResult LoadFromDiskCache()
    {
        try
        {
            if (File.Exists(CachePath))
            {
                var json = File.ReadAllText(CachePath);
                var rates = ParseRates(json);
                if (rates.Count > 0)
                {
                    var updated = new DateTimeOffset(File.GetLastWriteTimeUtc(CachePath), TimeSpan.Zero);
                    return new ExchangeRatesResult(rates, true, updated);
                }
            }
        }
        catch { }

        // Core fallback rates
        return new ExchangeRatesResult(new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1.0,
            ["EUR"] = 0.92,
            ["GBP"] = 0.79,
            ["JPY"] = 153.5,
            ["CAD"] = 1.39,
            ["AUD"] = 1.52,
            ["CHF"] = 0.88,
            ["CNY"] = 7.22,
            ["PHP"] = 58.70,
            ["SGD"] = 1.34,
            ["INR"] = 84.40
        }, true, null);
    }

    public static double Convert(double amount, string from, string to, Dictionary<string, double> rates)
    {
        if (!rates.TryGetValue(from, out var fromRate) || !rates.TryGetValue(to, out var toRate) || fromRate <= 0)
            return amount;

        // Base is USD: amount in USD = amount / fromRate; amount in target = (amount / fromRate) * toRate
        double amountInUsd = amount / fromRate;
        return amountInUsd * toRate;
    }
}
