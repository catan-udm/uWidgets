using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Services;
using uWidgets.Services;
using uWidgets.Views;
using uWidgets.Views.Pages;

namespace uWidgets;

public class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        StorageService.Initialize();

        _services = new ServiceCollection()
            .AddSingleton<IAppSettingsProvider, AppSettingsProvider>()
            .AddSingleton<ILayoutProvider, LayoutProvider>()
            .AddSingleton<IAssemblyProvider, AssemblyProvider>()
            .AddSingleton<IThemeService, ThemeService>()
            .AddSingleton<ILocaleService, LocaleService>()
            .AddSingleton<IGridService<Widget>, GridService>()
            .AddSingleton<WidgetRegistry>()
            .AddSingleton<IWidgetFactory<Window, UserControl>, WidgetFactory>()
            .AddSingleton<WidgetPlacementService>()
            .AddSingleton<HotkeyService>(sp => new HotkeyService(
                sp.GetRequiredService<IAppSettingsProvider>(),
                () => sp.GetRequiredService<WidgetRegistry>().ToggleAllVisibility(),
                () => ToggleLockPositions(sp.GetRequiredService<IAppSettingsProvider>()),
                () => OpenSettingsWindow()))
            .AddSingleton<Settings, Settings>()
            .AddSingleton<UpdateService, UpdateService>()
            .BuildServiceProvider();

        var appSettingsProvider = _services.GetRequiredService<IAppSettingsProvider>();
        var themeService = _services.GetRequiredService<IThemeService>();
        var localeService = _services.GetRequiredService<ILocaleService>();

        var appSettings = appSettingsProvider.Get();
        localeService.SetCulture(appSettings.Region.Language);
        themeService.Apply(appSettings.Theme);

        // Initialize global hotkeys early on the UI thread
        var hotkeyService = _services.GetRequiredService<HotkeyService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = appSettings.ShowTrayIcon
                ? ShutdownMode.OnExplicitShutdown
                : ShutdownMode.OnLastWindowClose;

            desktop.Exit += (_, _) => hotkeyService.Dispose();

            UpdateTrayVisibility(appSettings.ShowTrayIcon);
            appSettingsProvider.DataChanged += (_, oldSettings, newSettings) =>
            {
                if (oldSettings?.ShowTrayIcon != newSettings.ShowTrayIcon)
                {
                    desktop.ShutdownMode = newSettings.ShowTrayIcon
                        ? ShutdownMode.OnExplicitShutdown
                        : ShutdownMode.OnLastWindowClose;
                    UpdateTrayVisibility(newSettings.ShowTrayIcon);
                }
            };
        }

        var widgetsCount = _services
            .GetRequiredService<IWidgetFactory<Window, UserControl>>()
            .Create()
            .Select(widget =>
            {
                widget.Show();
                return widget;
            })
            .Count();

        if (!appSettings.EffectiveOnboarding.IsCompleted)
        {
            var welcome = new WelcomeModal(
                appSettingsProvider,
                _services.GetRequiredService<ILayoutProvider>(),
                _services.GetRequiredService<WidgetPlacementService>(),
                openSettings =>
                {
                    if (openSettings)
                        OpenSettingsWindow(typeof(WidgetManager));
                });
            welcome.Show();
        }
        else if ((ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Args.Length: > 0 } dt && dt.Args[0] == "--settings") || widgetsCount == 0)
        {
            OpenSettingsWindow();
        }

        if (appSettings.CheckUpdatesAutomatically)
            _services.GetRequiredService<UpdateService>().CheckForUpdates();

        base.OnFrameworkInitializationCompleted();
    }

    private void UpdateTrayVisibility(bool visible)
    {
        try
        {
            var icons = TrayIcon.GetIcons(this);
            if (icons != null)
            {
                foreach (var icon in icons)
                    icon.IsVisible = visible;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] TrayIcon update warning: {ex.Message}");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
        }
    }

    private void OpenSettingsWindow(Type? targetPage = null)
    {
        if (_services == null) return;
        var settings = _services.GetRequiredService<Settings>();
        if (targetPage != null)
            settings.NavigateToPage(targetPage);
        settings.ShowAndActivate();
    }

    private void AddQuickWidget(string type, string subType, int cols = 2, int rows = 2)
    {
        if (_services == null) return;
        var placementService = _services.GetRequiredService<WidgetPlacementService>();
        var widgetFactory = _services.GetRequiredService<IWidgetFactory<Window, UserControl>>();
        var settingsWin = _services.GetRequiredService<Settings>();
        var layout = placementService.CreateSmartLayout(settingsWin, type, subType, cols, rows);
        widgetFactory.Add(layout).Show();
    }

    private void Tray_OnClicked(object? sender, EventArgs e) => OpenSettingsWindow();
    private void Tray_OpenSettings(object? sender, EventArgs e) => OpenSettingsWindow();
    private void Tray_OpenHelp(object? sender, EventArgs e) => OpenSettingsWindow(typeof(HelpAndGuide));
    private void Tray_AddClock(object? sender, EventArgs e) => AddQuickWidget("Clock", "AnalogI", 2, 2);
    private void Tray_AddWeather(object? sender, EventArgs e) => AddQuickWidget("Weather", "Forecast", 4, 2);
    private void Tray_AddNotes(object? sender, EventArgs e) => AddQuickWidget("Notes", "Note", 2, 2);
    private void Tray_AddMonitor(object? sender, EventArgs e) => AddQuickWidget("Monitor", "Dashboard", 2, 2);
    private void Tray_AddMedia(object? sender, EventArgs e) => AddQuickWidget("Media", "NowPlaying", 4, 2);
    private void Tray_BrowseWidgets(object? sender, EventArgs e) => OpenSettingsWindow(typeof(WidgetManager));

    private void Tray_ToggleWidgets(object? sender, EventArgs e)
    {
        _services?.GetRequiredService<WidgetRegistry>().ToggleAllVisibility();
    }

    private void Tray_ToggleLock(object? sender, EventArgs e)
    {
        if (_services == null) return;
        ToggleLockPositions(_services.GetRequiredService<IAppSettingsProvider>());
    }

    private static void ToggleLockPositions(IAppSettingsProvider provider)
    {
        var current = provider.Get();
        var updated = current with
        {
            Layout = current.Layout with { LockPosition = !current.Layout.LockPosition }
        };
        provider.Save(updated);
    }

    private void Tray_Restart(object? sender, EventArgs e)
    {
        var executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(executablePath))
        {
            var process = Process.Start(executablePath, "--settings");
            var tryCount = 0;
            while (process != null && process.MainWindowHandle == IntPtr.Zero && !process.HasExited && tryCount++ < 10)
            {
                Thread.Sleep(100);
                process.Refresh();
            }
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void Tray_Exit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}