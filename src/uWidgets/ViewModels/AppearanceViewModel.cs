using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;
using uWidgets.Locales;
using uWidgets.Views.Controls;

namespace uWidgets.ViewModels;

public class AppearanceViewModel : ReactiveObject
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private bool nativeFrameChangedPendingRestart;

    public AppearanceViewModel(IAppSettingsProvider appSettingsProvider)
    {
        this.appSettingsProvider = appSettingsProvider;
        appSettingsProvider.DataChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object sender, AppSettings? oldData, AppSettings newData)
    {
        this.RaisePropertyChanged(nameof(DarkMode));
        this.RaisePropertyChanged(nameof(OpacityPercent));
        this.RaisePropertyChanged(nameof(OpacityPercentLabel));
        this.RaisePropertyChanged(nameof(AccentMode));
        this.RaisePropertyChanged(nameof(AccentColor));
        this.RaisePropertyChanged(nameof(ShowColorPalette));
        this.RaisePropertyChanged(nameof(Monochrome));
        this.RaisePropertyChanged(nameof(Font));
        this.RaisePropertyChanged(nameof(UseNativeFrame));
        this.RaisePropertyChanged(nameof(CornerRadius));
        this.RaisePropertyChanged(nameof(PreviewCornerRadius));
        this.RaisePropertyChanged(nameof(PreviewBackground));
        this.RaisePropertyChanged(nameof(PreviewForeground));
        this.RaisePropertyChanged(nameof(PreviewAccentBrush));
        this.RaisePropertyChanged(nameof(PreviewFontFamily));
        this.RaisePropertyChanged(nameof(PreviewBorderThickness));
    }

    public ThemeButton[] Themes => appSettingsProvider.Get().Templates.Select(theme => new ThemeButton(appSettingsProvider, theme)).ToArray();
    
    public DarkModeViewModel[] DarkModes =>
    [
        new DarkModeViewModel(Locale.Settings_Appearance_DarkMode_False, false),
        new DarkModeViewModel(Locale.Settings_Appearance_DarkMode_True, true),
        new DarkModeViewModel(Locale.Settings_Appearance_DarkMode_Null, null)
    ];

    public AccentColorViewModel[] AccentComboboxItems =>
    [
        new AccentColorViewModel(Locale.Settings_Appearance_AccentColor_Null, null),
        new AccentColorViewModel(Locale.Settings_Appearance_AccentColor_Manual, "#3376CD")
    ];

    public bool ShowColorPalette => appSettingsProvider.Get().Theme.AccentColor != null;
    
    public AccentColorViewModel AccentMode
    {
        get => appSettingsProvider.Get().Theme.AccentColor == null ? AccentComboboxItems[0] : AccentComboboxItems[1];
        set
        {
            var settings = appSettingsProvider.Get();
            var newTheme = settings.Theme with { AccentColor = value.Value };
            appSettingsProvider.Save(settings with { Theme = newTheme });
            this.RaisePropertyChanged(nameof(ShowColorPalette));
        }
    }

    public Color AccentColor
    {
        get => Color.TryParse(appSettingsProvider.Get().Theme.AccentColor, out var color) ? color : Colors.DodgerBlue;
        set
        {
            var settings = appSettingsProvider.Get();
            var newTheme = settings.Theme with { AccentColor = value.ToString() };
            appSettingsProvider.Save(settings with { Theme = newTheme });
        }
    }

    public DarkModeViewModel? DarkMode
    {
        get => DarkModes.FirstOrDefault(theme => theme.Value == appSettingsProvider.Get().Theme.DarkMode);
        set
        {
            var settings = appSettingsProvider.Get();
            var newTheme = settings.Theme with { DarkMode = value?.Value };
            appSettingsProvider.Save(settings with { Theme = newTheme });
        }
    }

    public int OpacityPercent
    {
        get => (int)Math.Round(appSettingsProvider.Get().Theme.OpacityLevel * 100);
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            var settings = appSettingsProvider.Get();
            var newTheme = settings.Theme with { OpacityLevel = Math.Round(clamped / 100.0, 2) };
            appSettingsProvider.Save(settings with { Theme = newTheme });
            this.RaisePropertyChanged(nameof(OpacityPercent));
            this.RaisePropertyChanged(nameof(OpacityPercentLabel));
        }
    }

    public string OpacityPercentLabel => $"{OpacityPercent}%";

    public int CornerRadius
    {
        get => appSettingsProvider.Get().Dimensions.Radius;
        set
        {
            var settings = appSettingsProvider.Get();
            var dimensions = settings.Dimensions with { Radius = value };
            appSettingsProvider.Save(settings with { Dimensions = dimensions });
            this.RaisePropertyChanged(nameof(CornerRadius));
            this.RaisePropertyChanged(nameof(PreviewCornerRadius));
        }
    }

    public bool Monochrome
    {
        get => appSettingsProvider.Get().Theme.Monochrome;
        set
        {
            var settings = appSettingsProvider.Get();
            var newTheme = settings.Theme with { Monochrome = value };
            appSettingsProvider.Save(settings with { Theme = newTheme });
        }
    }

    public List<string> Fonts => ["Inter", "Segoe UI", "Microsoft YaHei"];

    public string Font
    {
        get => appSettingsProvider.Get().Theme.FontFamily;
        set
        {
            var settings = appSettingsProvider.Get();
            var theme = settings.Theme with { FontFamily = value };
            appSettingsProvider.Save(settings with { Theme = theme });
        }
    }

    public bool NativeFrameChangedPendingRestart
    {
        get => nativeFrameChangedPendingRestart;
        private set => this.RaiseAndSetIfChanged(ref nativeFrameChangedPendingRestart, value);
    }

    public bool UseNativeFrame
    {
        get => appSettingsProvider.Get().Theme.UseNativeFrame;
        set
        {
            var settings = appSettingsProvider.Get();
            if (settings.Theme.UseNativeFrame == value) return;
            var theme = settings.Theme with
            {
                UseNativeFrame = value,
                LiquidGlass = value ? false : settings.Theme.LiquidGlass
            };
            appSettingsProvider.Save(settings with { Theme = theme });
            NativeFrameChangedPendingRestart = true;
        }
    }

    // Live Preview Properties
    public CornerRadius PreviewCornerRadius => UseNativeFrame ? new CornerRadius(2) : new CornerRadius(CornerRadius);
    public Thickness PreviewBorderThickness => new(UseNativeFrame ? 1.5 : 1);

    public IBrush PreviewBackground
    {
        get
        {
            var theme = appSettingsProvider.Get().Theme;
            var isDark = theme.DarkMode ?? (Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark);
            var baseColor = isDark ? Color.Parse("#2E2E2E") : Color.Parse("#FFFFFF");
            return new SolidColorBrush(baseColor, theme.LiquidGlass ? Math.Max(0.25, theme.OpacityLevel) : theme.OpacityLevel);
        }
    }

    public IBrush PreviewForeground
    {
        get
        {
            var isDark = appSettingsProvider.Get().Theme.DarkMode ?? (Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark);
            return new SolidColorBrush(isDark ? Colors.White : Color.Parse("#1F1F1F"));
        }
    }

    public IBrush PreviewAccentBrush
    {
        get
        {
            var theme = appSettingsProvider.Get().Theme;
            if (theme.Monochrome) return PreviewForeground;
            if (theme.AccentColor != null && Color.TryParse(theme.AccentColor, out var custom))
                return new SolidColorBrush(custom);
            return new SolidColorBrush(Color.Parse("#3376CD"));
        }
    }

    public FontFamily PreviewFontFamily => Font == "Inter"
        ? new FontFamily("avares://Avalonia.Fonts.Inter#Inter")
        : new FontFamily(Font);
}