using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;
using uWidgets.Locales;
using uWidgets.Services;

namespace uWidgets.Views.Pages;

public partial class About : UserControl, IDisposable
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly UpdateService updateService;
    private readonly CancellationTokenSource checkCts = new();
    public string? Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    public string DataFolderPath => Const.CurrentFolder;

    public About(IAppSettingsProvider appSettingsProvider, UpdateService updateService)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.updateService = updateService;
        DataContext = this;
        InitializeComponent();
        _ = CheckForUpdates();
    }

    private async Task CheckForUpdates()
    {
        try
        {
            var version = await updateService.GetUpdateVersionAsync(checkCts.Token);
            if (version == null || checkCts.IsCancellationRequested) return;

            Update.IsVisible = true;
            Update.Subtitle = string.Format(Locale.Update_UpdateFormat, version);
        }
        catch (Exception)
        {
            // Ignore transient/cancellation update check failures on the About page
        }
    }
    
    private void GoToUpdate(object? sender, RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://www.github.com/creewick/uWidgets/releases/latest") { UseShellExecute = true });
    }

    private void GoToRepository(object? sender, RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://www.github.com/creewick/uWidgets") { UseShellExecute = true });
    }

    private void GoToIssues(object? sender, RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://github.com/creewick/uWidgets/issues") { UseShellExecute = true });
    }

    private void OpenDataFolder(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.CurrentFolder);
        Process.Start(new ProcessStartInfo(Const.CurrentFolder) { UseShellExecute = true });
    }

    private void OpenLogsFolder(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.LogsFolder);
        Process.Start(new ProcessStartInfo(Const.LogsFolder) { UseShellExecute = true });
    }

    private void ResetDefaults(object? sender, RoutedEventArgs e)
    {
        var current = appSettingsProvider.Get();
        var defaults = AppSettings.CreateDefault() with { Onboarding = current.EffectiveOnboarding };
        appSettingsProvider.Save(defaults);
    }

    public void Dispose()
    {
        checkCts.Cancel();
        checkCts.Dispose();
        GC.SuppressFinalize(this);
    }
}