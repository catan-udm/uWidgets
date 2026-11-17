using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

public class GeneralViewModel : ReactiveObject, IDisposable
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly HotkeyService? hotkeyService;
    private bool languageChangedPendingRestart;
    private bool disposed;

    public GeneralViewModel(IAppSettingsProvider appSettingsProvider, HotkeyService? hotkeyService = null)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.hotkeyService = hotkeyService;

        if (hotkeyService != null)
            hotkeyService.WarningsChanged += OnHotkeyWarningsChanged;
    }

    private void OnHotkeyWarningsChanged(object? sender, EventArgs e)
    {
        this.RaisePropertyChanged(nameof(HotkeyWarningText));
        this.RaisePropertyChanged(nameof(HasHotkeyWarnings));
    }

    private static readonly Lazy<CultureInfo[]> CachedLanguages = new(() =>
        GetAvailableCultures().ToHashSet().OrderBy(x => x.DisplayName).ToArray());

    public CultureInfo[] Languages => CachedLanguages.Value;

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
            if (!new StartupService().SetRunOnStartup(value))
            {
                this.RaisePropertyChanged(nameof(RunOnStartup));
                return;
            }

            var settings = appSettingsProvider.Get();
            appSettingsProvider.Save(settings with { RunOnStartup = value });
            this.RaisePropertyChanged(nameof(RunOnStartup));
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
        new(Locale.Layering_DesktopPinned, LayeringMode.DesktopPinned),
        new(Locale.Layering_Normal, LayeringMode.Normal),
        new(Locale.Layering_AlwaysOnTop, LayeringMode.AlwaysOnTop)
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

    public static IEnumerable<CultureInfo> GetAvailableCultures()
    {
        yield return new CultureInfo("en");

        // Probe only directories that actually contain satellite resource assemblies to avoid hundreds of disk misses on the UI thread.
        var assemblyLocation = typeof(Locale).Assembly.Location;
        var assemblyDir = string.IsNullOrEmpty(assemblyLocation) ? null : Path.GetDirectoryName(assemblyLocation);
        var satelliteFileName = $"{typeof(Locale).Assembly.GetName().Name}.resources.dll";

        IEnumerable<CultureInfo> candidates;
        if (!string.IsNullOrEmpty(assemblyDir) && Directory.Exists(assemblyDir))
        {
            candidates = Directory.EnumerateDirectories(assemblyDir)
                .Where(dir => File.Exists(Path.Combine(dir, satelliteFileName)))
                .Select(dir =>
                {
                    try { return CultureInfo.GetCultureInfo(Path.GetFileName(dir)); }
                    catch (CultureNotFoundException) { return null; }
                })
                .Where(c => c != null)!;
        }
        else
        {
            candidates = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        }

        foreach (var culture in candidates)
        {
            if (culture.Equals(CultureInfo.InvariantCulture) ||
                string.Equals(culture.Name, "en", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ResourceSet? resourceSet = null;

            try
            {
                resourceSet = Locale.ResourceManager.GetResourceSet(culture, true, false);
            }
            catch (CultureNotFoundException)
            {
            }

            if (resourceSet != null && resourceSet.Cast<DictionaryEntry>().Any())
                yield return culture;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (hotkeyService != null)
            hotkeyService.WarningsChanged -= OnHotkeyWarningsChanged;
        GC.SuppressFinalize(this);
    }
}