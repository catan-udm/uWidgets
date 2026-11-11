using System.IO;
using System.Net.Http;
using System.Text.Json;
using uWidgets.Core;

namespace Finance.Services;

public static class CurrencyExchangeService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly string CachePath = Path.Combine(Const.CurrentFolder, "finance_exchange_rates.json");

    private static Dictionary<string, double>? cachedRates;

    public static async Task<Dictionary<string, double>> GetExchangeRatesAsync()
    {
        if (cachedRates != null && cachedRates.Count > 0)
            return cachedRates;

        try
        {
            using var response = await HttpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var rates = ParseRates(json);
                if (rates.Count > 0)
                {
                    cachedRates = rates;
                    _ = File.WriteAllTextAsync(CachePath, json);
                    return rates;
                }
            }
        }
        catch
        {
            // Offline
        }

        cachedRates = LoadFromDiskCache();
        return cachedRates;
    }

    private static Dictionary<string, double> ParseRates(string json)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("rates", out var ratesProp))
        {
            foreach (var prop in ratesProp.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetDouble();
            }
        }
        return dict;
    }

    private static Dictionary<string, double> LoadFromDiskCache()
    {
        try
        {
            if (File.Exists(CachePath))
            {
                var json = File.ReadAllText(CachePath);
                var rates = ParseRates(json);
                if (rates.Count > 0) return rates;
            }
        }
        catch { }

        // Core fallback rates
        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
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
            ["SGD"] = 1.34
        };
    }

    public static double Convert(double amount, string from, string to, Dictionary<string, double> rates)
    {
        if (!rates.TryGetValue(from, out var fromRate) || !rates.TryGetValue(to, out var toRate))
            return amount;

        // Base is USD: amount in USD = amount / fromRate; amount in target = (amount / fromRate) * toRate
        double amountInUsd = amount / fromRate;
        return amountInUsd * toRate;
    }
}
