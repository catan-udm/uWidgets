using System.IO;
using System.Net.Http;
using System.Text.Json;
using Finance.Models;
using uWidgets.Core;

namespace Finance.Services;

public static class StockPriceService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly string CachePath = Path.Combine(Const.CurrentFolder, "finance_stocks_cache.json");

    static StockPriceService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public static async Task<List<StockQuoteInfo>> GetStockQuotesAsync(List<string> symbols)
    {
        var list = new List<StockQuoteInfo>();

        foreach (var sym in symbols)
        {
            var quote = await FetchQuoteAsync(sym);
            if (quote != null)
            {
                list.Add(quote);
            }
        }

        if (list.Count > 0)
        {
            try
            {
                var json = JsonSerializer.Serialize(list);
                _ = File.WriteAllTextAsync(CachePath, json);
            }
            catch { }
            return list;
        }

        return LoadFromDiskCache();
    }

    private static async Task<StockQuoteInfo?> FetchQuoteAsync(string symbol)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range=2d";

        try
        {
            using var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var meta = result.GetProperty("meta");

            var regularMarketPrice = meta.GetProperty("regularMarketPrice").GetDouble();
            var previousClose = meta.TryGetProperty("chartPreviousClose", out var prevProp)
                ? prevProp.GetDouble()
                : (meta.TryGetProperty("previousClose", out var prev2) ? prev2.GetDouble() : regularMarketPrice);

            var shortName = meta.TryGetProperty("shortName", out var nameProp)
                ? nameProp.GetString() ?? symbol
                : GetFriendlySymbolName(symbol);

            double change = regularMarketPrice - previousClose;
            double percentChange = previousClose > 0 ? (change / previousClose) * 100.0 : 0.0;

            return new StockQuoteInfo(symbol, shortName, regularMarketPrice, change, percentChange);
        }
        catch
        {
            return null;
        }
    }

    private static string GetFriendlySymbolName(string symbol) => symbol switch
    {
        "^GSPC" => "S&P 500",
        "^IXIC" => "NASDAQ",
        "^DJI" => "Dow Jones",
        "AAPL" => "Apple",
        "NVDA" => "NVIDIA",
        "MSFT" => "Microsoft",
        "GOOGL" => "Alphabet",
        "AMZN" => "Amazon",
        "TSLA" => "Tesla",
        _ => symbol
    };

    private static List<StockQuoteInfo> LoadFromDiskCache()
    {
        try
        {
            if (File.Exists(CachePath))
            {
                var json = File.ReadAllText(CachePath);
                var cached = JsonSerializer.Deserialize<List<StockQuoteInfo>>(json);
                if (cached != null && cached.Count > 0) return cached;
            }
        }
        catch { }

        // Minimal seed data for offline state
        return
        [
            new StockQuoteInfo("^GSPC", "S&P 500", 5980.50, 22.40, 0.38),
            new StockQuoteInfo("^IXIC", "NASDAQ", 19280.00, 115.30, 0.60),
            new StockQuoteInfo("NVDA", "NVIDIA", 148.20, 3.80, 2.63),
            new StockQuoteInfo("AAPL", "Apple", 228.10, -1.20, -0.52)
        ];
    }
}
