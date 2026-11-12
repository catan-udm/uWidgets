namespace uWidgets.Core.Models.Settings;

/// <summary>
/// Tracks first-run onboarding state and starter pack selection.
/// </summary>
public record OnboardingSettings(
    bool IsCompleted = false,
    bool ShowWelcomeOnStartup = true,
    string? SelectedStarterPack = null,
    int Version = 1);

/// <summary>
/// Configurable global Windows keyboard shortcuts.
/// </summary>
public record HotkeySettings(
    bool Enabled = true,
    string ToggleWidgets = "Win+Shift+W",
    string ToggleLock = "Win+Alt+L",
    string OpenSettings = "Ctrl+Shift+U");

/// <summary>
/// Application settings, stored in <c>appSettings.json</c>.
/// </summary>
public record AppSettings(
    Theme Theme,
    Theme[] Templates,
    Layout Layout,
    Dimensions Dimensions,
    Region Region,
    bool RunOnStartup,
    string? IgnoreUpdate,
    OnboardingSettings? Onboarding = null,
    HotkeySettings? Hotkeys = null,
    bool ShowTrayIcon = true,
    bool CheckUpdatesAutomatically = true)
{
    /// <summary>
    /// Returns guaranteed non-null onboarding settings.
    /// </summary>
    public OnboardingSettings EffectiveOnboarding => Onboarding ?? new OnboardingSettings();

    /// <summary>
    /// Returns guaranteed non-null hotkey settings.
    /// </summary>
    public HotkeySettings EffectiveHotkeys => Hotkeys ?? new HotkeySettings();

    /// <summary>
    /// Builds safe default settings when <c>appSettings.json</c> is missing or unreadable.
    /// </summary>
    public static AppSettings CreateDefault()
    {
        Theme[] templates =
        [
            new(false, null, 0.4, true, false, "Inter", false),
            new(false, null, 0.4, true, true, "Segoe UI", false),
            new(false, null, 1.0, true, false, "Inter", false),
            new(false, null, 1.0, true, true, "Segoe UI", false),
            new(true, null, 0.4, true, false, "Inter", false),
            new(true, null, 0.4, true, true, "Segoe UI", false),
            new(true, null, 1.0, true, false, "Inter", false),
            new(true, null, 1.0, true, true, "Segoe UI", false)
        ];

        return new AppSettings(
            Theme: new Theme(null, null, 0.4, true, false, "Inter", false),
            Templates: templates,
            Layout: new Layout(true, false, true, false, LayeringMode.DesktopPinned),
            Dimensions: new Dimensions(72, 16, 24),
            Region: new Region("en"),
            RunOnStartup: false,
            IgnoreUpdate: null,
            Onboarding: new OnboardingSettings(),
            Hotkeys: new HotkeySettings(),
            ShowTrayIcon: true,
            CheckUpdatesAutomatically: true);
    }
}
