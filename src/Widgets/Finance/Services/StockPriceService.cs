using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Finance.Models;
using uWidgets.Core;

namespace Finance.Services;

public record StockQuotesResult(List<StockQuoteInfo> Quotes, bool IsOfflineFallback);

public static class StockPriceService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly string CachePath = Path.Combine(Const.CurrentFolder, "finance_stocks_cache.json");
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    static StockPriceService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public static async Task<List<StockQuoteInfo>> GetStockQuotesAsync(List<string> symbols) =>
        (await GetStockQuotesWithStatusAsync(symbols)).Quotes;

    public static async Task<StockQuotesResult> GetStockQuotesWithStatusAsync(List<string> symbols)
    {
        var cleanSymbols = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleanSymbols.Count == 0) return new StockQuotesResult([], false);

        var fetched = await Task.WhenAll(cleanSymbols.Select(FetchQuoteAsync));
        var list = fetched.Where(q => q != null).Select(q => q!).ToList();

        if (list.Count > 0)
        {
            await WriteCacheSafeAsync(list);
            return new StockQuotesResult(list, false);
        }

        return LoadFromDiskCache(cleanSymbols);
    }

    private static async Task WriteCacheSafeAsync(List<StockQuoteInfo> quotes)
    {
        await CacheLock.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(CachePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(quotes);
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

    private static async Task<StockQuoteInfo?> FetchQuoteAsync(string symbol)
    {
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=2d";

        try
        {
            using var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("chart", out var chartProp) ||
                !chartProp.TryGetProperty("result", out var resultArr) ||
                resultArr.ValueKind != JsonValueKind.Array ||
                resultArr.GetArrayLength() == 0)
            {
                return null;
            }

            var result = resultArr[0];
            if (!result.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object)
                return null;

            if (!meta.TryGetProperty("regularMarketPrice", out var priceProp) ||
                priceProp.ValueKind != JsonValueKind.Number ||
                !priceProp.TryGetDouble(out var regularMarketPrice))
            {
                return null;
            }

            double previousClose = regularMarketPrice;
            if (meta.TryGetProperty("chartPreviousClose", out var prevProp) &&
                prevProp.ValueKind == JsonValueKind.Number &&
                prevProp.TryGetDouble(out var p1))
            {
                previousClose = p1;
            }
            else if (meta.TryGetProperty("previousClose", out var prev2) &&
                     prev2.ValueKind == JsonValueKind.Number &&
                     prev2.TryGetDouble(out var p2))
            {
                previousClose = p2;
            }

            var shortName = meta.TryGetProperty("shortName", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString() ?? symbol
                : GetFriendlySymbolName(symbol);

            var currency = meta.TryGetProperty("currency", out var curProp) && curProp.ValueKind == JsonValueKind.String
                ? curProp.GetString() ?? "USD"
                : "USD";

            double change = regularMarketPrice - previousClose;
            double percentChange = previousClose > 0 ? (change / previousClose) * 100.0 : 0.0;

            return new StockQuoteInfo(symbol, shortName, regularMarketPrice, change, percentChange, currency);
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

    private static StockQuotesResult LoadFromDiskCache(List<string> requestedSymbols)
    {
        try
        {
            if (File.Exists(CachePath))
            {
                var json = File.ReadAllText(CachePath);
                var cached = JsonSerializer.Deserialize<List<StockQuoteInfo>>(json);
                if (cached != null)
                {
                    var matched = cached.Where(c => requestedSymbols.Contains(c.Symbol, StringComparer.OrdinalIgnoreCase)).ToList();
                    if (matched.Count > 0) return new StockQuotesResult(matched, true);
                }
            }
        }
        catch { }

        // Minimal seed data only for default tickers when offline
        StockQuoteInfo[] seeds =
        [
            new StockQuoteInfo("^GSPC", "S&P 500", 5980.50, 22.40, 0.38, "USD"),
            new StockQuoteInfo("^IXIC", "NASDAQ", 19280.00, 115.30, 0.60, "USD"),
            new StockQuoteInfo("NVDA", "NVIDIA", 148.20, 3.80, 2.63, "USD"),
            new StockQuoteInfo("AAPL", "Apple", 228.10, -1.20, -0.52, "USD"),
            new StockQuoteInfo("MSFT", "Microsoft", 422.50, 1.90, 0.45, "USD")
        ];
        var matchedSeeds = seeds.Where(s => requestedSymbols.Contains(s.Symbol, StringComparer.OrdinalIgnoreCase)).ToList();
        return new StockQuotesResult(matchedSeeds, true);
    }
}
