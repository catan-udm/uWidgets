using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using uWidgets.Services;
using Weather.Models;
using Weather.Models.Forecast;
using Weather.Services;

namespace Weather.ViewModels;

public class ForecastViewModel : ReactiveObject, IDisposable
{
    private readonly ForecastModel model;
    private readonly OpenMeteoWeatherProvider provider;
    private readonly DispatcherTimer cooldownTimer;
    private int cooldownSecondsRemaining;
    private bool isFetching;
    private DateTime? lastSuccessAt;

    public ForecastViewModel(ForecastModel model)
    {
        provider = new OpenMeteoWeatherProvider();
        this.model = model;

        cooldownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        cooldownTimer.Tick += OnCooldownTick;

        RetryCommand = ReactiveCommand.CreateFromTask(() => UpdateForecastAsync(forceRefresh: true), outputScheduler: RxApp.MainThreadScheduler);
        RefreshCommand = ReactiveCommand.CreateFromTask(() => UpdateForecastAsync(forceRefresh: true), outputScheduler: RxApp.MainThreadScheduler);

        TimerService.Timer1Hour.Subscribe(UpdateForecast);
        UpdateForecast();
    }

    public ICommand RetryCommand { get; }
    public ICommand RefreshCommand { get; }

    private void OnCooldownTick(object? sender, EventArgs e)
    {
        if (cooldownSecondsRemaining > 0)
        {
            cooldownSecondsRemaining--;
            this.RaisePropertyChanged(nameof(CanRetry));
            this.RaisePropertyChanged(nameof(RetryButtonText));
            UpdateStatusText();
        }

        if (cooldownSecondsRemaining <= 0)
        {
            cooldownTimer.Stop();
        }
    }

    private void UpdateForecast() => _ = UpdateForecastAsync(forceRefresh: false);

    private async Task UpdateForecastAsync(bool forceRefresh)
    {
        if (isFetching) return;
        isFetching = true;
        Dispatcher.UIThread.Post(() => this.RaisePropertyChanged(nameof(CanRetry)));

        try
        {
            var result = await provider.GetForecastWithStatusAsync(model.Latitude, model.Longitude, model.TemperatureUnit, forceRefresh);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Forecast is not { } forecast || forecast.Current == null || forecast.Hourly == null || forecast.Daily == null)
                {
                    HasErrorBanner = true;
                    isRateLimited = result.IsRateLimited;
                    if (result.IsRateLimited)
                    {
                        StartRateLimitCooldown();
                    }
                    else
                    {
                        UpdateStatusText();
                    }

                    if (!lastSuccessAt.HasValue)
                    {
                        CurrentCondition = result.IsRateLimited ? "Rate limited" : "Offline";
                    }

                    return;
                }

                isRateLimited = result.IsRateLimited;
                HasErrorBanner = result.IsCached || result.IsOffline || result.IsRateLimited;
                lastSuccessAt = result.CachedAtLocalTime ?? DateTime.Now;
                LastUpdatedText = $"Updated {lastSuccessAt.Value:HH:mm}";
                UpdateStatusText();

                var cityNow = forecast.Current.Time != default ? forecast.Current.Time : DateTime.Now;
                var upcomingIndices = GetUpcomingHourlyIndices(forecast, cityNow, 24);
                var currentHourIndex = upcomingIndices.Count > 0 ? upcomingIndices[0] : 0;

                var unitSuffix = string.Equals(model.TemperatureUnit, "fahrenheit", StringComparison.OrdinalIgnoreCase)
                    ? "°F"
                    : "°C";

                var dailyMinCount = forecast.Daily.Min?.Count ?? 0;
                var dailyMaxCount = forecast.Daily.Max?.Count ?? 0;
                var todayMin = dailyMinCount > 0 ? forecast.Daily.Min![0] : forecast.Current.Temperature;
                var todayMax = dailyMaxCount > 0 ? forecast.Daily.Max![0] : forecast.Current.Temperature;
                var periodMin = dailyMinCount > 0 ? forecast.Daily.Min!.Min() : todayMin;
                var periodMax = dailyMaxCount > 0 ? forecast.Daily.Max!.Max() : todayMax;

                var isDayNow = forecast.Hourly.IsDay is { Count: > 0 } &&
                               currentHourIndex < forecast.Hourly.IsDay.Count
                    ? forecast.Hourly.IsDay[currentHourIndex] > 0
                    : true;

                var currentUv = forecast.Hourly.UVIndex is { Count: > 0 } &&
                                currentHourIndex < forecast.Hourly.UVIndex.Count
                    ? forecast.Hourly.UVIndex[currentHourIndex]
                    : 0;

                CurrentTemperature = $"{forecast.Current.Temperature:0}°";
                CurrentIcon = isDayNow ? WeatherIconProvider.GetIcon(forecast.Current.WeatherCode) : WeatherIcon.Night.Value;
                CurrentCondition = GetCondition(forecast.Current.WeatherCode) ?? forecast.Current.WeatherCode.ToString();
                CurrentMinMax = $"{todayMax:0}°  {todayMin:0}°";
                CurrentMin = $"{todayMin:0}";
                CurrentMax = $"{todayMax:0}";

                TemperatureMetric = new(
                    periodMin,
                    periodMax,
                    forecast.Current.Temperature,
                    null,
                    $"{CityName} • {forecast.Current.Temperature:0.#}{unitSuffix} (Range {periodMin:0.#}..{periodMax:0.#}{unitSuffix})");

                UVIndex = new(
                    0,
                    10,
                    currentUv,
                    WeatherIcon.Clear.Value,
                    $"{CityName} • UV Index: {currentUv:0.#}");

                Pressure = new(
                    960,
                    1050,
                    forecast.Current.Pressure,
                    WeatherIcon.Pressure.Value,
                    $"{CityName} • Pressure: {forecast.Current.Pressure:0.#} hPa");

                SunsetSunrise = GetSunsetSunrise(forecast, cityNow);

                HourlyForecast = upcomingIndices
                    .Select(idx => GetHourlyForecast(forecast, idx))
                    .ToArray();

                var dailyCount = new[]
                {
                    forecast.Daily.Time?.Count ?? 0,
                    forecast.Daily.Min?.Count ?? 0,
                    forecast.Daily.Max?.Count ?? 0,
                    forecast.Daily.WeatherCode?.Count ?? 0
                }.Min();

                DailyForecast = Enumerable
                    .Range(0, dailyCount)
                    .Select(day => GetDailyForecast(forecast, day, periodMin, periodMax))
                    .ToArray();
            });
        }
        finally
        {
            isFetching = false;
            Dispatcher.UIThread.Post(() => this.RaisePropertyChanged(nameof(CanRetry)));
        }
    }

    public static IReadOnlyList<int> GetUpcomingHourlyIndices(ForecastResponse forecast, DateTime cityNow, int count = 24)
    {
        if (forecast.Hourly == null || forecast.Hourly.Temperature == null || forecast.Hourly.Temperature.Count == 0)
            return Array.Empty<int>();

        var maxCount = new[]
        {
            forecast.Hourly.Temperature.Count,
            forecast.Hourly.WeatherCode?.Count ?? 0,
            forecast.Hourly.IsDay?.Count ?? 0
        }.Min();

        if (maxCount <= 0) return Array.Empty<int>();

        int startIndex = 0;
        var times = forecast.Hourly.Time;
        if (times is { Count: > 0 })
        {
            var targetHour = new DateTime(cityNow.Year, cityNow.Month, cityNow.Day, cityNow.Hour, 0, 0);
            int limit = Math.Min(times.Count, maxCount);
            bool found = false;
            for (int i = 0; i < limit; i++)
            {
                if (times[i] >= targetHour)
                {
                    startIndex = i;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                startIndex = Math.Max(0, limit - 1);
            }
        }
        else
        {
            startIndex = Math.Clamp(cityNow.Hour, 0, maxCount - 1);
        }

        var takeCount = Math.Min(count, Math.Max(0, maxCount - startIndex));
        return Enumerable.Range(startIndex, takeCount).ToArray();
    }

    private void StartRateLimitCooldown()
    {
        cooldownSecondsRemaining = 30;
        cooldownTimer.Stop();
        cooldownTimer.Start();
        this.RaisePropertyChanged(nameof(CanRetry));
        this.RaisePropertyChanged(nameof(RetryButtonText));
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (!HasErrorBanner)
        {
            StatusBadgeText = string.Empty;
            StatusTooltip = lastSuccessAt.HasValue ? $"{CityName} • Last updated {lastSuccessAt.Value:HH:mm}" : CityName;
            return;
        }

        if (isRateLimited && cooldownSecondsRemaining > 0)
        {
            StatusBadgeText = lastSuccessAt.HasValue
                ? $"Last updated {lastSuccessAt.Value:HH:mm} (Cached)"
                : $"Rate limited ({cooldownSecondsRemaining}s)";
            StatusTooltip = $"{StatusBadgeText} • Retry available in {cooldownSecondsRemaining}s";
        }
        else if (lastSuccessAt.HasValue)
        {
            StatusBadgeText = $"Last updated {lastSuccessAt.Value:HH:mm} (Cached)";
            StatusTooltip = $"Offline — showing cached weather from {lastSuccessAt.Value:HH:mm}";
        }
        else
        {
            StatusBadgeText = isRateLimited ? "Rate limited — try again" : "Offline — unavailable";
            StatusTooltip = StatusBadgeText;
        }
    }

    private SunriseSunsetViewModel GetSunsetSunrise(ForecastResponse forecast, DateTime cityNow)
    {
        if (forecast.Daily.Sunrise == null || forecast.Daily.Sunrise.Count == 0 ||
            forecast.Daily.Sunset == null || forecast.Daily.Sunset.Count == 0)
        {
            return new(WeatherIcon.Sunrise.Value, "--:--", $"{CityName} • Sunrise/Sunset unavailable");
        }

        var sunrise = forecast.Daily.Sunrise[0];
        var sunset = forecast.Daily.Sunset[0];
        var sunriseTomorrow = forecast.Daily.Sunrise.Count > 1 ? forecast.Daily.Sunrise[1] : sunrise.AddDays(1);

        if (cityNow < sunrise)
            return new(WeatherIcon.Sunrise.Value, sunrise.ToString("HH:mm"), $"{CityName} • Sunrise at {sunrise:HH:mm}");
        if (cityNow < sunset)
            return new(WeatherIcon.Sunset.Value, sunset.ToString("HH:mm"), $"{CityName} • Sunset at {sunset:HH:mm}");
        return new(WeatherIcon.Sunrise.Value, sunriseTomorrow.ToString("HH:mm"), $"{CityName} • Next Sunrise at {sunriseTomorrow:HH:mm}");
    }

    private DailyForecastViewModel GetDailyForecast(ForecastResponse forecast, int day, double totalMin, double totalMax)
    {
        var dayOfWeek = (int)forecast.Daily.Time[day].DayOfWeek;
        var dayOfWeekName = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.AbbreviatedDayNames[dayOfWeek];
        var code = forecast.Daily.WeatherCode[day];
        var icon = WeatherIconProvider.GetIcon(code);
        var min = forecast.Daily.Min[day];
        var max = forecast.Daily.Max[day];

        double span = totalMax - totalMin;
        List<GridLength> graph = span <= 0
            ? [new GridLength(0, GridUnitType.Star), new GridLength(1, GridUnitType.Star), new GridLength(0, GridUnitType.Star)]
            : [
                new GridLength(Math.Max(0, min - totalMin), GridUnitType.Star),
                new GridLength(Math.Max(0.01, max - min), GridUnitType.Star),
                new GridLength(Math.Max(0, totalMax - max), GridUnitType.Star),
            ];

        return new DailyForecastViewModel(dayOfWeekName, icon, $"{min:0}°", $"{max:0}°", graph);
    }

    private HourlyForecastViewModel GetHourlyForecast(ForecastResponse forecast, int index)
    {
        var time = forecast.Hourly.Time != null && index < forecast.Hourly.Time.Count
            ? forecast.Hourly.Time[index].Hour.ToString()
            : (index % 24).ToString();
        var code = forecast.Hourly.WeatherCode[index];
        var icon = forecast.Hourly.IsDay[index] > 0 ? WeatherIconProvider.GetIcon(code) : WeatherIcon.Night.Value;
        var temperature = forecast.Hourly.Temperature[index];

        return new HourlyForecastViewModel(time, icon, $"{temperature:0}°");
    }
    
    private string? GetCondition(WeatherCode code) => Locales.Locale.ResourceManager.GetString($"Weather_Code_{(int)code}");

    private StreamGeometry currentIcon = new();
    public StreamGeometry CurrentIcon
    {
        get => currentIcon;
        private set => this.RaiseAndSetIfChanged(ref currentIcon, value);
    }

    private IEnumerable<HourlyForecastViewModel> hourlyForecast = [];
    public IEnumerable<HourlyForecastViewModel> HourlyForecast
    {
        get => hourlyForecast;
        private set => this.RaiseAndSetIfChanged(ref hourlyForecast, value);
    }

    private IEnumerable<DailyForecastViewModel> dailyForecast = [];
    public IEnumerable<DailyForecastViewModel> DailyForecast
    {
        get => dailyForecast;
        private set => this.RaiseAndSetIfChanged(ref dailyForecast, value);
    }
    
    public string CityName => model.Name;

    private string? currentTemperature = "--°";
    public string? CurrentTemperature
    {
        get => currentTemperature;
        private set => this.RaiseAndSetIfChanged(ref currentTemperature, value);
    }
    
    private string? currentCondition = "--------";
    public string? CurrentCondition
    {
        get => currentCondition;
        private set => this.RaiseAndSetIfChanged(ref currentCondition, value);
    }
    
    private string? currentMinMax = "--°  --°";
    public string? CurrentMinMax
    {
        get => currentMinMax;
        private set => this.RaiseAndSetIfChanged(ref currentMinMax, value);
    }
        
    private string? currentMin = "--";
    public string? CurrentMin
    {
        get => currentMin;
        private set => this.RaiseAndSetIfChanged(ref currentMin, value);
    }
    
    private string? currentMax = "--";
    public string? CurrentMax
    {
        get => currentMax;
        private set => this.RaiseAndSetIfChanged(ref currentMax, value);
    }

    private MetricViewModel temperatureMetric = new(0, 1, 0, null);
    public MetricViewModel TemperatureMetric
    {
        get => temperatureMetric;
        private set => this.RaiseAndSetIfChanged(ref temperatureMetric, value);
    }
    
    private MetricViewModel uvIndex = new(0, 10, 0, WeatherIcon.Clear.Value);
    public MetricViewModel UVIndex
    {
        get => uvIndex;
        private set => this.RaiseAndSetIfChanged(ref uvIndex, value);
    }

    private MetricViewModel pressure = new(960, 1050, 960, WeatherIcon.Pressure.Value);
    public MetricViewModel Pressure
    {
        get => pressure;
        private set => this.RaiseAndSetIfChanged(ref pressure, value);
    }
    
    private SunriseSunsetViewModel sunsetSunrise = new(new(), "--:--");
    public SunriseSunsetViewModel SunsetSunrise
    {
        get => sunsetSunrise;
        private set => this.RaiseAndSetIfChanged(ref sunsetSunrise, value);
    }

    private bool isRateLimited;

    private bool hasErrorBanner;
    public bool HasErrorBanner
    {
        get => hasErrorBanner;
        private set => this.RaiseAndSetIfChanged(ref hasErrorBanner, value);
    }

    private string statusBadgeText = string.Empty;
    public string StatusBadgeText
    {
        get => statusBadgeText;
        private set => this.RaiseAndSetIfChanged(ref statusBadgeText, value);
    }

    private string statusTooltip = string.Empty;
    public string StatusTooltip
    {
        get => statusTooltip;
        private set => this.RaiseAndSetIfChanged(ref statusTooltip, value);
    }

    private string lastUpdatedText = string.Empty;
    public string LastUpdatedText
    {
        get => lastUpdatedText;
        private set => this.RaiseAndSetIfChanged(ref lastUpdatedText, value);
    }

    public bool CanRetry => cooldownSecondsRemaining <= 0 && !isFetching;
    public string RetryButtonText => cooldownSecondsRemaining > 0 ? $"Retry ({cooldownSecondsRemaining}s)" : "Retry";

    public void Dispose()
    {
        cooldownTimer.Stop();
        cooldownTimer.Tick -= OnCooldownTick;
        TimerService.Timer1Hour.Unsubscribe(UpdateForecast);
        GC.SuppressFinalize(this);
    }
}