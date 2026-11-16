using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using uWidgets.Core;
using Weather.Models.Forecast;
using Weather.Models.Geocoding;

namespace Weather.Services;

public record WeatherFetchResult(
    ForecastResponse? Forecast,
    bool IsRateLimited = false,
    bool IsOffline = false,
    bool IsCached = false,
    DateTime? CachedAtLocalTime = null,
    string? ErrorMessage = null);

public class OpenMeteoWeatherProvider
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly string CacheDirectory = Path.Combine(Const.CurrentFolder, "weather_cache");
    private static readonly ConcurrentDictionary<string, (ForecastResponse Forecast, DateTime FetchedAtLocal)> MemoryForecastCache = new();
    private static readonly ConcurrentDictionary<string, (AirQualityResponse Aqi, DateTime FetchedAtLocal)> MemoryAqiCache = new();
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    static OpenMeteoWeatherProvider()
    {
        SharedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("uWidgets/1.0 (Windows; OpenSource)");
    }

    public async Task<ForecastResponse?> GetForecastAsync(double latitude, double longitude, string temperatureUnit, bool forceRefresh = false)
    {
        var result = await GetForecastWithStatusAsync(latitude, longitude, temperatureUnit, forceRefresh);
        return result.Forecast;
    }

    public async Task<WeatherFetchResult> GetForecastWithStatusAsync(double latitude, double longitude, string temperatureUnit, bool forceRefresh = false)
    {
        var latitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", latitude);
        var longitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", longitude);
        var normalizedUnit = string.IsNullOrWhiteSpace(temperatureUnit) ? "celsius" : temperatureUnit.Trim().ToLowerInvariant();
        var cacheKey = $"{latitudeString}_{longitudeString}_{normalizedUnit}";

        if (!forceRefresh &&
            MemoryForecastCache.TryGetValue(cacheKey, out var memCached) &&
            DateTime.Now - memCached.FetchedAtLocal < TimeSpan.FromMinutes(10))
        {
            return new WeatherFetchResult(memCached.Forecast, IsCached: false, CachedAtLocalTime: memCached.FetchedAtLocal);
        }

        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?" +
                      $"latitude={latitudeString}&" +
                      $"longitude={longitudeString}&" +
                      $"temperature_unit={Uri.EscapeDataString(normalizedUnit)}&" +
                      $"current=temperature_2m,weathercode,surface_pressure&" +
                      $"hourly=temperature_2m,weathercode,uv_index,is_day&" +
                      $"daily=temperature_2m_min,temperature_2m_max,weathercode,sunrise,sunset&" +
                      $"timezone=auto";

            using var response = await SharedHttpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return await FallbackForecastAsync(cacheKey, isRateLimited: true, isOffline: false, "Rate limit exceeded (HTTP 429).");
            }

            if (!response.IsSuccessStatusCode)
            {
                await WriteWeatherLogAsync($"HTTP {(int)response.StatusCode} ({response.StatusCode}) for forecast request.");
                return await FallbackForecastAsync(cacheKey, isRateLimited: false, isOffline: true, $"Server returned {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var forecast = JsonSerializer.Deserialize<ForecastResponse>(json);
            if (forecast == null || forecast.Error || forecast.Current == null || forecast.Hourly == null || forecast.Daily == null)
            {
                var reason = forecast?.Reason ?? "Invalid forecast response.";
                return await FallbackForecastAsync(cacheKey, isRateLimited: false, isOffline: true, reason);
            }

            MemoryForecastCache[cacheKey] = (forecast, DateTime.Now);
            await WriteCacheFileAsync($"forecast_{cacheKey}.json", json);

            return new WeatherFetchResult(forecast);
        }
        catch (Exception e)
        {
            await WriteWeatherLogAsync(FormatExceptionForLog(e));
            return await FallbackForecastAsync(cacheKey, isRateLimited: false, isOffline: true, e.Message);
        }
    }

    private static async Task<WeatherFetchResult> FallbackForecastAsync(string cacheKey, bool isRateLimited, bool isOffline, string errorMessage)
    {
        if (MemoryForecastCache.TryGetValue(cacheKey, out var memCached))
        {
            return new WeatherFetchResult(memCached.Forecast, isRateLimited, isOffline, IsCached: true, CachedAtLocalTime: memCached.FetchedAtLocal, ErrorMessage: errorMessage);
        }

        var disk = await ReadCacheFileAsync<ForecastResponse>($"forecast_{cacheKey}.json");
        if (disk.Data != null && !disk.Data.Error && disk.Data.Current != null)
        {
            MemoryForecastCache[cacheKey] = (disk.Data, disk.UpdatedAtLocal);
            return new WeatherFetchResult(disk.Data, isRateLimited, isOffline, IsCached: true, CachedAtLocalTime: disk.UpdatedAtLocal, ErrorMessage: errorMessage);
        }

        return new WeatherFetchResult(null, isRateLimited, isOffline, ErrorMessage: errorMessage);
    }

    public async Task<AirQualityResponse?> GetAirQualityAsync(double latitude, double longitude, bool forceRefresh = false)
    {
        var latitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", latitude);
        var longitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", longitude);
        var cacheKey = $"{latitudeString}_{longitudeString}";

        if (!forceRefresh &&
            MemoryAqiCache.TryGetValue(cacheKey, out var memCached) &&
            DateTime.Now - memCached.FetchedAtLocal < TimeSpan.FromMinutes(10))
        {
            return memCached.Aqi;
        }

        try
        {
            var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?" +
                      $"latitude={latitudeString}&" +
                      $"longitude={longitudeString}&" +
                      $"current=european_aqi";

            using var response = await SharedHttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await WriteWeatherLogAsync($"HTTP {(int)response.StatusCode} ({response.StatusCode}) for air quality request.");
                return (await ReadCacheFileAsync<AirQualityResponse>($"aqi_{cacheKey}.json")).Data;
            }

            var json = await response.Content.ReadAsStringAsync();
            var aqi = JsonSerializer.Deserialize<AirQualityResponse>(json);
            if (aqi != null && !aqi.Error && aqi.Current != null)
            {
                MemoryAqiCache[cacheKey] = (aqi, DateTime.Now);
                await WriteCacheFileAsync($"aqi_{cacheKey}.json", json);
                return aqi;
            }

            return (await ReadCacheFileAsync<AirQualityResponse>($"aqi_{cacheKey}.json")).Data;
        }
        catch (Exception e)
        {
            await WriteWeatherLogAsync(FormatExceptionForLog(e));
            return MemoryAqiCache.TryGetValue(cacheKey, out var cached)
                ? cached.Aqi
                : (await ReadCacheFileAsync<AirQualityResponse>($"aqi_{cacheKey}.json")).Data;
        }
    }

    public async Task<List<City>?> GetCitiesAsync(string cityName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return [];

        try
        {
            var language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            var url = $"https://geocoding-api.open-meteo.com/v1/search?" +
                      $"name={Uri.EscapeDataString(cityName)}&" +
                      $"language={Uri.EscapeDataString(language)}";

            using var response = await SharedHttpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResults = JsonSerializer.Deserialize<GeocodingResponse>(json);

            return searchResults?.Cities;
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (Exception e)
        {
            await WriteWeatherLogAsync(FormatExceptionForLog(e));
            return null;
        }
    }

    private static async Task WriteCacheFileAsync(string fileName, string json)
    {
        await CacheLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            var path = Path.Combine(CacheDirectory, fileName);
            await File.WriteAllTextAsync(path, json);
        }
        catch
        {
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private static async Task<(T? Data, DateTime UpdatedAtLocal)> ReadCacheFileAsync<T>(string fileName)
    {
        await CacheLock.WaitAsync();
        try
        {
            var path = Path.Combine(CacheDirectory, fileName);
            if (!File.Exists(path)) return (default, DateTime.Now);
            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<T>(json);
            return (data, File.GetLastWriteTime(path));
        }
        catch
        {
            return (default, DateTime.Now);
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private const long MaxWeatherLogBytes = 64 * 1024;
    private static readonly SemaphoreSlim LogLock = new(1, 1);

    private static string FormatExceptionForLog(Exception e) =>
        e is HttpRequestException or TaskCanceledException
            ? $"{e.GetType().Name}: {e.Message}"
            : $"{e.Message}{Environment.NewLine}{e.StackTrace}";

    private static async Task WriteWeatherLogAsync(string content)
    {
        await LogLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(Const.LogsFolder);
            var logPath = Path.Combine(Const.LogsFolder, "weather_crash_log.txt");

            if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxWeatherLogBytes)
            {
                var lines = await File.ReadAllLinesAsync(logPath);
                await File.WriteAllLinesAsync(logPath, lines.Skip(Math.Max(0, lines.Length / 2)));
            }

            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {content}{Environment.NewLine}{Environment.NewLine}";
            await File.AppendAllTextAsync(logPath, entry);
        }
        catch
        {
            // Logging failures should never crash the weather widget.
        }
        finally
        {
            LogLock.Release();
        }
    }
}