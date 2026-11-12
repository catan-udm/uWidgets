using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using ReactiveUI;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;
using uWidgets.Core.Services;
using uWidgets.Locales;
using uWidgets.Services;

namespace uWidgets.ViewModels;

public record LayeringModeOption(string Name, LayeringMode Value);

public class GeneralViewModel : ReactiveObject
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly HotkeyService? hotkeyService;
    private bool languageChangedPendingRestart;

    public GeneralViewModel(IAppSettingsProvider appSettingsProvider, HotkeyService? hotkeyService = null)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.hotkeyService = hotkeyService;

        if (hotkeyService != null)
            hotkeyService.WarningsChanged += (_, _) =>
            {
                this.RaisePropertyChanged(nameof(HotkeyWarningText));
                this.RaisePropertyChanged(nameof(HasHotkeyWarnings));
            };
    }

    public CultureInfo[] Languages => GetAvailableCultures().ToHashSet().OrderBy(x => x.DisplayName).ToArray();

    public bool LanguageChangedPendingRestart
    {
        get => languageChangedPendingRestart;
        private set => this.RaiseAndSetIfChanged(ref languageChangedPendingRestart, value);
    }

    public CultureInfo Language
    {
        get => CultureInfo.GetCultureInfo(appSettingsProvider.Get().Region.Language);
        set
        {
            var settings = appSettingsProvider.Get();
            if (string.Equals(settings.Region.Language, value.Name, StringComparison.OrdinalIgnoreCase))
                return;

            var newRegion = settings.Region with { Language = value.Name };
            var newSettings = settings with { Region = newRegion };
            appSettingsProvider.Save(newSettings);
            LanguageChangedPendingRestart = true;
        }
    }

    public bool RunOnStartup
    {
        get => appSettingsProvider.Get().RunOnStartup;
        set
        {
            if (!new StartupService().SetRunOnStartup(value)) return;

            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { RunOnStartup = value });
        }
    }

    public bool ShowTrayIcon
    {
        get => appSettingsProvider.Get().ShowTrayIcon;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { ShowTrayIcon = value });
        }
    }

    public bool CheckUpdatesAutomatically
    {
        get => appSettingsProvider.Get().CheckUpdatesAutomatically;
        set
        {
            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { CheckUpdatesAutomatically = value });
        }
    }

    public LayeringModeOption[] LayeringModes { get; } =
    [
        new("Desktop Pinned (Behind Windows)", LayeringMode.DesktopPinned),
        new("Normal Floating Window", LayeringMode.Normal),
        new("Always on Top", LayeringMode.AlwaysOnTop)
    ];

    public LayeringModeOption SelectedLayeringMode
    {
        get => LayeringModes.FirstOrDefault(m => m.Value == appSettingsProvider.Get().Layout.LayeringMode) ?? LayeringModes[0];
        set
        {
            var settings = appSettingsProvider.Get();
            var newLayout = settings.Layout with { LayeringMode = value.Value };
            appSettingsProvider.Save(settings with { Layout = newLayout });
            this.RaisePropertyChanged(nameof(SelectedLayeringMode));
        }
    }

    public bool HotkeysEnabled
    {
        get => appSettingsProvider.Get().EffectiveHotkeys.Enabled;
        set
        {
            var settings = appSettingsProvider.Get();
            var hotkeys = settings.EffectiveHotkeys with { Enabled = value };
            appSettingsProvider.Save(settings with { Hotkeys = hotkeys });
            this.RaisePropertyChanged(nameof(HotkeysEnabled));
        }
    }

    public string ToggleWidgetsHotkey
    {
        get => appSettingsProvider.Get().EffectiveHotkeys.ToggleWidgets;
        set
        {
            var settings = appSettingsProvider.Get();
            var hotkeys = settings.EffectiveHotkeys with { ToggleWidgets = value.Trim() };
            appSettingsProvider.Save(settings with { Hotkeys = hotkeys });
        }
    }

    public string ToggleLockHotkey
    {
        get => appSettingsProvider.Get().EffectiveHotkeys.ToggleLock;
        set
        {
            var settings = appSettingsProvider.Get();
            var hotkeys = settings.EffectiveHotkeys with { ToggleLock = value.Trim() };
            appSettingsProvider.Save(settings with { Hotkeys = hotkeys });
        }
    }

    public string OpenSettingsHotkey
    {
        get => appSettingsProvider.Get().EffectiveHotkeys.OpenSettings;
        set
        {
            var settings = appSettingsProvider.Get();
            var hotkeys = settings.EffectiveHotkeys with { OpenSettings = value.Trim() };
            appSettingsProvider.Save(settings with { Hotkeys = hotkeys });
        }
    }

    public bool HasHotkeyWarnings => hotkeyService?.RegistrationWarnings.Count > 0;

    public string HotkeyWarningText => hotkeyService == null
        ? string.Empty
        : string.Join(Environment.NewLine, hotkeyService.RegistrationWarnings);

    public void ResetHotkeysToDefault()
    {
        var settings = appSettingsProvider.Get();
        appSettingsProvider.Save(settings with { Hotkeys = new HotkeySettings() });
        this.RaisePropertyChanged(nameof(HotkeysEnabled));
        this.RaisePropertyChanged(nameof(ToggleWidgetsHotkey));
        this.RaisePropertyChanged(nameof(ToggleLockHotkey));
        this.RaisePropertyChanged(nameof(OpenSettingsHotkey));
    }

    private static IEnumerable<CultureInfo> GetAvailableCultures()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

        yield return new CultureInfo("en");

        foreach (var culture in cultures)
        {
            if (culture.Equals(CultureInfo.InvariantCulture)) continue;
            ResourceSet? resourceSet = null;

            try
            {
                resourceSet = Locale.ResourceManager.GetResourceSet(culture, true, false);
            }
            catch (CultureNotFoundException)
            {
            }

            if (resourceSet != null)
                yield return culture;
        }
    }
}