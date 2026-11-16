using Calendar.ViewModels;
using Clock.Services;
using Finance.Services;
using Weather.Models.Forecast;
using Weather.ViewModels;
using Xunit;

namespace uWidgets.Tests;

public class WidgetLogicTests
{
    [Theory]
    // 2024-09-01 is Sunday
    [InlineData(2024, 9, DayOfWeek.Sunday, 0)]
    [InlineData(2024, 9, DayOfWeek.Monday, 6)]
    [InlineData(2024, 9, DayOfWeek.Saturday, 1)]
    // 2026-01-01 is Thursday
    [InlineData(2026, 1, DayOfWeek.Sunday, 4)]
    [InlineData(2026, 1, DayOfWeek.Monday, 3)]
    // 2026-06-01 is Monday
    [InlineData(2026, 6, DayOfWeek.Sunday, 1)]
    [InlineData(2026, 6, DayOfWeek.Monday, 0)]
    public void MonthCalendar_ComputeLeadingEmptyDays_HandlesAllWeekStartDays(
        int year, int month, DayOfWeek firstDayOfWeek, int expected)
    {
        Assert.Equal(expected, MonthCalendarViewModel.ComputeLeadingEmptyDays(year, month, firstDayOfWeek));
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(300, "+05:00")]
    [InlineData(-300, "-05:00")]
    [InlineData(330, "+05:30")]   // India UTC+05:30
    [InlineData(-210, "-03:30")]  // Newfoundland UTC-03:30
    [InlineData(345, "+05:45")]   // Nepal UTC+05:45
    [InlineData(-570, "-09:30")]  // Marquesas UTC-09:30
    public void ClockTimeZoneHelper_FormatTimeZoneDiff_HandlesFractionalAndZeroOffsets(
        int diffMinutes, string expected)
    {
        var formatted = ClockTimeZoneHelper.FormatTimeZoneDiff(TimeSpan.FromMinutes(diffMinutes));
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void ClockTimeZoneHelper_FindSafeTimeZone_InvalidIdFallsBackToLocal()
    {
        var tz = ClockTimeZoneHelper.FindSafeTimeZone("NonExistent/Fake_TimeZone_Id");
        Assert.Equal(TimeZoneInfo.Local.Id, tz.Id);
    }

    [Theory]
    [InlineData(0.0, 100.0, 0.0, 21.0)]
    [InlineData(0.0, 100.0, 100.0, 0.0)]
    [InlineData(0.0, 100.0, 50.0, 10.5)]
    [InlineData(0.0, 100.0, -25.0, 21.0)]
    [InlineData(0.0, 100.0, 150.0, 0.0)]
    [InlineData(15.0, 15.0, 15.0, 21.0)]  // Equal min/max guard (no NaN)
    [InlineData(20.0, 10.0, 15.0, 21.0)]  // Inverted bounds guard
    public void WeatherMetricViewModel_StrokeDashOffset_CorrectDirectionAndSafeBounds(
        double min, double max, double value, double expectedOffset)
    {
        var vm = new MetricViewModel(min, max, value, null);
        Assert.Equal(expectedOffset, vm.StrokeDashOffset, precision: 6);
        Assert.False(double.IsNaN(vm.StrokeDashOffset));
        Assert.False(double.IsInfinity(vm.StrokeDashOffset));
    }

    [Fact]
    public void ForecastViewModel_GetUpcomingHourlyIndices_CrossesMidnightWithoutModuloWrap()
    {
        var startDay = new DateTime(2026, 6, 1, 0, 0, 0);
        var times = Enumerable.Range(0, 72).Select(h => startDay.AddHours(h)).ToList();
        var forecast = new ForecastResponse
        {
            Current = new CurrentForecast { Time = new DateTime(2026, 6, 1, 22, 15, 0) },
            Hourly = new HourlyForecast
            {
                Time = times,
                Temperature = Enumerable.Range(0, 72).Select(h => (double)h).ToList(),
                UVIndex = Enumerable.Repeat(1.0, 72).ToList(),
                WeatherCode = Enumerable.Repeat(WeatherCode.ClearSky, 72).ToList(),
                IsDay = Enumerable.Repeat(1, 72).ToList()
            }
        };

        var indices = ForecastViewModel.GetUpcomingHourlyIndices(forecast, forecast.Current.Time, 24);

        Assert.Equal(24, indices.Count);
        Assert.Equal(22, indices[0]);
        Assert.Equal(23, indices[1]);
        Assert.Equal(24, indices[2]); // Next day hour 00, NOT wrapped back to index 0!
        Assert.Equal(45, indices[23]);
    }

    [Fact]
    public void ForecastViewModel_GetUpcomingHourlyIndices_TruncatesSafelyNearEndOfSeries()
    {
        var startDay = new DateTime(2026, 6, 1, 0, 0, 0);
        var times = Enumerable.Range(0, 30).Select(h => startDay.AddHours(h)).ToList();
        var forecast = new ForecastResponse
        {
            Current = new CurrentForecast { Time = startDay.AddHours(25) },
            Hourly = new HourlyForecast
            {
                Time = times,
                Temperature = Enumerable.Repeat(20.0, 30).ToList(),
                UVIndex = Enumerable.Repeat(0.0, 30).ToList(),
                WeatherCode = Enumerable.Repeat(WeatherCode.ClearSky, 30).ToList(),
                IsDay = Enumerable.Repeat(1, 30).ToList()
            }
        };

        var indices = ForecastViewModel.GetUpcomingHourlyIndices(forecast, forecast.Current.Time, 24);

        Assert.Equal([25, 26, 27, 28, 29], indices);
    }

    [Theory]
    [InlineData(16.0, 16.0, 1.0)]
    [InlineData(20.0, 10.0, 2.0)]
    [InlineData(30.0, 20.0, 1.5)]
    [InlineData(32.0, 0.0, 2.0)]   // Invalid basePx falls back to 16
    public void UnitConverterService_PxToRemAndRemToPx_UsesCustomRootFontSize(
        double px, double basePx, double expectedRem)
    {
        var rem = UnitConverterService.PxToRem(px, basePx);
        Assert.Equal(expectedRem, rem, precision: 6);

        var roundTripPx = UnitConverterService.RemToPx(rem, basePx);
        Assert.Equal(px, roundTripPx, precision: 6);
    }

    [Fact]
    public void UnitConverterService_ConvertStorageBytes_ConvertsAccurately()
    {
        Assert.Equal(1.0, UnitConverterService.ConvertStorageBytes(1024, "MB", "GB"), precision: 6);
        Assert.Equal(1024.0 * 1024.0, UnitConverterService.ConvertStorageBytes(1, "MB", "B"), precision: 6);
    }
}
