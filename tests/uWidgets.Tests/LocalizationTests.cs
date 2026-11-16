using uWidgets.Locales;
using uWidgets.ViewModels;
using Xunit;

namespace uWidgets.Tests;

public class LocalizationTests
{
    [Fact]
    public void GetAvailableCultures_FiltersEmptyPacksAndIncludesActiveCultures()
    {
        var cultures = GeneralViewModel.GetAvailableCultures()
            .Select(c => c.Name)
            .ToList();

        Assert.Contains("en", cultures);
        Assert.Contains("ru", cultures);
        Assert.Contains("zh-Hans", cultures);
        Assert.Contains("zh-Hant", cultures);
        Assert.Contains("de", cultures);

        // Ensure "en" appears once only
        Assert.Equal(1, cultures.Count(c => string.Equals(c, "en", StringComparison.OrdinalIgnoreCase)));

        // Empty 0-key packs must be filtered out
        Assert.DoesNotContain("es", cultures);
        Assert.DoesNotContain("fr", cultures);
        Assert.DoesNotContain("ja", cultures);
        Assert.DoesNotContain("ko", cultures);
        Assert.DoesNotContain("pl", cultures);
        Assert.DoesNotContain("uk", cultures);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("de")]
    public void ActiveCultures_ContainLiquidGlassAndHostUiStrings(string cultureName)
    {
        var culture = new System.Globalization.CultureInfo(cultureName);
        Assert.False(string.IsNullOrWhiteSpace(Locale.ResourceManager.GetString(nameof(Locale.Settings_Appearance_LiquidGlass_Title), culture)));
        Assert.False(string.IsNullOrWhiteSpace(Locale.ResourceManager.GetString(nameof(Locale.Settings_Appearance_LiquidGlass_Subtitle), culture)));
        Assert.False(string.IsNullOrWhiteSpace(Locale.ResourceManager.GetString(nameof(Locale.Tray_OpenSettings), culture)));
        Assert.False(string.IsNullOrWhiteSpace(Locale.ResourceManager.GetString(nameof(Locale.Widget_Duplicate), culture)));
        Assert.False(string.IsNullOrWhiteSpace(Locale.ResourceManager.GetString(nameof(Locale.Layering_DesktopPinned), culture)));
    }
}
