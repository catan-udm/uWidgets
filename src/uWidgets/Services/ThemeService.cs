using System;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;

namespace uWidgets.Services;

public class ThemeService : IThemeService
{
    public ThemeService(IAppSettingsProvider appSettingsProvider)
    {
        appSettingsProvider.DataChanging += (_, _, newSettings) => 
            Apply(newSettings.Theme);
    }
    
    private readonly StyleInclude transparentStyle = new(new Uri("avares://uWidgets/"))
    {
        Source = new Uri("avares://uWidgets/Styles/Transparent.axaml")
    };
    
    private readonly StyleInclude monochromeStyle = new(new Uri("avares://uWidgets/"))
    {
        Source = new Uri("avares://uWidgets/Styles/Monochrome.axaml")
    };

    private readonly StyleInclude liquidGlassStyle = new(new Uri("avares://uWidgets/"))
    {
        Source = new Uri("avares://uWidgets/Styles/LiquidGlass.axaml")
    };
    
    public void Apply(Theme theme)
    {
        Application.Current!.RequestedThemeVariant = theme.DarkMode switch
        {
            null => ThemeVariant.Default,
            false => ThemeVariant.Light,
            _ => ThemeVariant.Dark,
        };
        
        Application.Current.Resources["BackgroundOpacity"] = theme.OpacityLevel;
        Application.Current.Resources["LiquidGlassRefThickness"] = theme.RefThickness;
        Application.Current.Resources["LiquidGlassRefFactor"] = theme.RefFactor;
        Application.Current.Resources["LiquidGlassRefDispersion"] = theme.RefDispersion;
        Application.Current.Resources["LiquidGlassShapeRadius"] = theme.ShapeRadius;
        Application.Current.Resources["LiquidGlassShapeRoundness"] = theme.ShapeRoundness;
        Application.Current.Resources["FontFamily"] = theme.FontFamily == "Inter"
            ? new FontFamily("avares://Avalonia.Fonts.Inter#Inter")
            : new FontFamily(theme.FontFamily);

        string[] accentKeys =
        [
            "SystemAccentColor",
            "SystemAccentColorDark1",
            "SystemAccentColorDark2",
            "SystemAccentColorDark3",
            "SystemAccentColorLight1",
            "SystemAccentColorLight2",
            "SystemAccentColorLight3"
        ];

        if (theme.AccentColor != null && Color.TryParse(theme.AccentColor, out var color))
        {
            foreach (var key in accentKeys)
            {
                Application.Current.Resources[key] = color;
            }
        }
        else
        {
            foreach (var key in accentKeys)
            {
                Application.Current.Resources.Remove(key);
            }
        }
        
        SwitchStyle(transparentStyle, theme.OpacityLevel < 1 || theme.LiquidGlass);
        SwitchStyle(monochromeStyle, theme.Monochrome);
        SwitchStyle(liquidGlassStyle, theme.LiquidGlass);
    }

    private static void SwitchStyle(StyleInclude style, bool enable)
    {
        if (enable && !Application.Current!.Styles.Contains(style))
            Application.Current.Styles.Add(style);
        if (!enable && Application.Current!.Styles.Contains(style))
            Application.Current.Styles.Remove(style);
    }
}