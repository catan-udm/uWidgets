using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
                () => ToggleMasterLock(sp.GetRequiredService<IAppSettingsProvider>()),
                () => OpenSettingsWindow()))
            .AddSingleton<SettingsWindowHost>()
            .AddTransient<Settings, Settings>()
            .AddSingleton<UpdateService, UpdateService>()
            .BuildServiceProvider();

        var appSettingsProvider = _services.GetRequiredService<IAppSettingsProvider>();
        var themeService = _services.GetRequiredService<IThemeService>();
        var localeService = _services.GetRequiredService<ILocaleService>();

        var appSettings = appSettingsProvider.Get();
        localeService.SetCulture(appSettings.Region.Language);
        LocalizeTrayIcons();
        themeService.Apply(appSettings.Theme);

        // Initialize global hotkeys early on the UI thread
        _ = _services.GetRequiredService<HotkeyService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = appSettings.ShowTrayIcon
                ? ShutdownMode.OnExplicitShutdown
                : ShutdownMode.OnLastWindowClose;

            desktop.Exit += (_, _) =>
            {
                SingleInstanceService.StopPipeServer();
                _services?.Dispose();
                SingleInstanceService.ReleaseMutex();
            };

            SingleInstanceService.StartPipeServer(() => Dispatcher.UIThread.Post(() => OpenSettingsWindow()));

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
        else if ((ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Args.Length: > 0 } dt && dt.Args.Contains("--settings")) || widgetsCount == 0)
        {
            OpenSettingsWindow();
        }

        if (appSettings.CheckUpdatesAutomatically)
            _services.GetRequiredService<UpdateService>().CheckForUpdates();

        base.OnFrameworkInitializationCompleted();
    }

    private void LocalizeTrayIcons()
    {
        try
        {
            var icons = TrayIcon.GetIcons(this);
            if (icons == null) return;

            foreach (var icon in icons)
            {
                icon.ToolTipText = uWidgets.Locales.Locale.Tray_ToolTip;
                icon.Menu = BuildTrayMenu();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] TrayIcon localize warning: {ex.Message}");
        }
    }

    private NativeMenu BuildTrayMenu()
    {
        var openSettings = new NativeMenuItem(uWidgets.Locales.Locale.Tray_OpenSettings);
        openSettings.Click += Tray_OpenSettings;

        var addClock = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Quick_Clock);
        addClock.Click += Tray_AddClock;
        var addWeather = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Quick_Weather);
        addWeather.Click += Tray_AddWeather;
        var addNotes = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Quick_Notes);
        addNotes.Click += Tray_AddNotes;
        var addMonitor = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Quick_Monitor);
        addMonitor.Click += Tray_AddMonitor;
        var addMedia = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Quick_Media);
        addMedia.Click += Tray_AddMedia;
        var browseAll = new NativeMenuItem(uWidgets.Locales.Locale.Tray_BrowseAll);
        browseAll.Click += Tray_BrowseWidgets;

        var addSubMenu = new NativeMenu();
        addSubMenu.Items.Add(addClock);
        addSubMenu.Items.Add(addWeather);
        addSubMenu.Items.Add(addNotes);
        addSubMenu.Items.Add(addMonitor);
        addSubMenu.Items.Add(addMedia);
        addSubMenu.Items.Add(new NativeMenuItemSeparator());
        addSubMenu.Items.Add(browseAll);

        var addWidgetItem = new NativeMenuItem(uWidgets.Locales.Locale.Tray_AddWidget) { Menu = addSubMenu };

        var toggleWidgets = new NativeMenuItem(uWidgets.Locales.Locale.Tray_ToggleWidgets);
        toggleWidgets.Click += Tray_ToggleWidgets;
        var toggleLock = new NativeMenuItem(uWidgets.Locales.Locale.Tray_ToggleLock);
        toggleLock.Click += Tray_ToggleLock;

        var help = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Help);
        help.Click += Tray_OpenHelp;
        var restart = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Restart);
        restart.Click += Tray_Restart;
        var exit = new NativeMenuItem(uWidgets.Locales.Locale.Tray_Exit);
        exit.Click += Tray_Exit;

        var menu = new NativeMenu();
        menu.Items.Add(openSettings);
        menu.Items.Add(addWidgetItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(toggleWidgets);
        menu.Items.Add(toggleLock);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(help);
        menu.Items.Add(restart);
        menu.Items.Add(exit);
        return menu;
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
        var settings = _services.GetRequiredService<SettingsWindowHost>().GetOrCreate();
        if (targetPage != null)
            settings.NavigateToPage(targetPage);
        settings.ShowAndActivate();
    }

    private void AddQuickWidget(string type, string subType, int cols = 2, int rows = 2)
    {
        if (_services == null) return;
        try
        {
            var placementService = _services.GetRequiredService<WidgetPlacementService>();
            var widgetFactory = _services.GetRequiredService<IWidgetFactory<Window, UserControl>>();
            var registry = _services.GetRequiredService<WidgetRegistry>();
            var settingsHost = _services.GetRequiredService<SettingsWindowHost>();
            Window referenceWindow = settingsHost.CurrentIfAlive
                ?? (Window?)registry.ActiveWidgets.FirstOrDefault()
                ?? settingsHost.GetOrCreate();
            var layout = placementService.CreateSmartLayout(referenceWindow, type, subType, cols, rows);
            widgetFactory.Add(layout).Show();
        }
        catch (Exception ex)
        {
            Program.AppendCrashLog($"[{DateTime.UtcNow:O}] AddQuickWidget({type}.{subType}) failed: {ex.Message}");
        }
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
        ToggleMasterLock(_services.GetRequiredService<IAppSettingsProvider>());
    }

    private static void ToggleMasterLock(IAppSettingsProvider provider)
    {
        var current = provider.Get();
        var nextLock = !(current.Layout.LockPosition && current.Layout.LockSize);
        var updated = current with
        {
            Layout = current.Layout with { LockPosition = nextLock, LockSize = nextLock }
        };
        provider.Save(updated);
    }

    private void Tray_Restart(object? sender, EventArgs e) =>
        AppRestartHelper.RestartApplication();

    private void Tray_Exit(object? sender, EventArgs e) =>
        AppRestartHelper.ShutdownApplication();
}