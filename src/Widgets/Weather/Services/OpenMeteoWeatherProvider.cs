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
    string? ErrorMessage = null);

public class OpenMeteoWeatherProvider
{
    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public async Task<ForecastResponse?> GetForecastAsync(double latitude, double longitude, string temperatureUnit)
    {
        var result = await GetForecastWithStatusAsync(latitude, longitude, temperatureUnit);
        return result.Forecast;
    }

    public async Task<WeatherFetchResult> GetForecastWithStatusAsync(double latitude, double longitude, string temperatureUnit)
    {
        try
        {
            var latitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", latitude);
            var longitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", longitude);

            var url = $"https://api.open-meteo.com/v1/forecast?" +
                      $"latitude={latitudeString}&" +
                      $"longitude={longitudeString}&" +
                      $"temperature_unit={temperatureUnit}&" +
                      $"current=temperature_2m,weathercode,surface_pressure&" +
                      $"hourly=temperature_2m,weathercode,uv_index,is_day&" +
                      $"daily=temperature_2m_min,temperature_2m_max,weathercode,sunrise,sunset&" +
                      $"timezone=auto";

            var response = await httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new WeatherFetchResult(null, IsRateLimited: true, ErrorMessage: "Rate limit exceeded (HTTP 429).");
            }

            if (!response.IsSuccessStatusCode)
            {
                await WriteWeatherLogAsync($"HTTP {(int)response.StatusCode} ({response.StatusCode}) for forecast request.");
                return new WeatherFetchResult(null, IsOffline: true, ErrorMessage: $"Server returned {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var forecast = JsonSerializer.Deserialize<ForecastResponse>(json);

            return new WeatherFetchResult(forecast);
        }
        catch (Exception e)
        {
            await WriteWeatherLogAsync($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            return new WeatherFetchResult(null, IsOffline: true, ErrorMessage: e.Message);
        }
    }

    public async Task<AirQualityResponse?> GetAirQualityAsync(double latitude, double longitude)
    {
        try
        {
            var latitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", latitude);
            var longitudeString = string.Format(CultureInfo.InvariantCulture, "{0:F4}", longitude);

            var url = $"https://air-quality-api.open-meteo.com/v1/air-quality?" +
                      $"latitude={latitudeString}&" +
                      $"longitude={longitudeString}&" +
                      $"current=european_aqi";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) throw new Exception($"HTTP {(int)response.StatusCode} ({response.StatusCode})");

            var json = await response.Content.ReadAsStringAsync();
            var forecast = JsonSerializer.Deserialize<AirQualityResponse>(json);

            return forecast;
        }
        catch (Exception e)
        {
            await WriteWeatherLogAsync($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            return null;
        }
    }

    public async Task<List<City>?> GetCitiesAsync(string cityName)
    {
        try
        {
            var language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            var url = $"https://geocoding-api.open-meteo.com/v1/search?" +
                      $"name={Uri.EscapeDataString(cityName)}&" +
                      $"language={language}";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<GeocodingResponse>(json);

            return searchResults?.Cities;
        }
        catch (Exception e)
        {
            await WriteWeatherLogAsync($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            return null;
        }
    }

    private static async Task WriteWeatherLogAsync(string content)
    {
        try
        {
            Directory.CreateDirectory(Const.LogsFolder);
            var logPath = Path.Combine(Const.LogsFolder, "weather_crash_log.txt");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {content}{Environment.NewLine}{Environment.NewLine}";
            await File.AppendAllTextAsync(logPath, entry);
        }
        catch
        {
            // Logging failures should never crash the weather widget.
        }
    }
}