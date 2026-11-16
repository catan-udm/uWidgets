using System;
using System.Linq;

namespace Clock.Services;

public static class ClockTimeZoneHelper
{
    public static TimeZoneInfo[] CachedTimeZones { get; } = TimeZoneInfo.GetSystemTimeZones().ToArray();

    public static TimeZoneInfo FindSafeTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Local;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    public static string FormatTimeZoneDiff(TimeSpan diff)
    {
        var absMinutes = Math.Abs((int)Math.Round(diff.TotalMinutes));
        if (absMinutes == 0)
            return "00:00";

        var abs = TimeSpan.FromMinutes(absMinutes);
        var sign = diff.TotalMinutes > 0 ? "+" : "-";
        return $"{sign}{(int)abs.TotalHours:D2}:{abs.Minutes:D2}";
    }

    public static string GetCityFullName(TimeZoneInfo tz)
    {
        var raw = tz.DisplayName.Split(") ").Last().Split(", ").First().Split('(').First().Trim();
        return string.IsNullOrWhiteSpace(raw) ? tz.StandardName : raw;
    }

    public static string GetCityAbbreviation(TimeZoneInfo tz)
    {
        if (tz.Id == TimeZoneInfo.Local.Id)
            return string.Empty;

        var full = GetCityFullName(tz);
        var letters = new string(full.Where(char.IsLetterOrDigit).ToArray());
        if (letters.Length >= 3)
            return letters[..3].ToUpperInvariant();
        if (letters.Length > 0)
            return letters.ToUpperInvariant();
        return "UTC";
    }
}
