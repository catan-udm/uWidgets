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
using uWidgets.Core.Services;
using uWidgets.Locales;
using uWidgets.Services;

namespace uWidgets.Views.Pages;

public partial class About : UserControl, IDisposable
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly UpdateService updateService;
    private readonly CancellationTokenSource checkCts = new();
    private bool resetConfirmPending;
    public string? Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    public string DataFolderPath => Const.CurrentFolder;

    public About(IAppSettingsProvider appSettingsProvider, UpdateService updateService)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.updateService = updateService;
        DataContext = this;
        InitializeComponent();
        RefreshSkippedBanner();
        _ = CheckForUpdatesAsync(manual: false);
    }

    private void RefreshSkippedBanner()
    {
        var ignored = appSettingsProvider.Get().IgnoreUpdate;
        if (!string.IsNullOrWhiteSpace(ignored))
        {
            UpdateStatusBanner.IsVisible = true;
            UpdateStatusBanner.Subtitle = $"Notifications for version {ignored} are currently skipped.";
            ClearSkipButton.IsVisible = true;
        }
        else
        {
            ClearSkipButton.IsVisible = false;
        }
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (manual)
        {
            CheckUpdatesButton.IsEnabled = false;
            UpdateStatusBanner.IsVisible = true;
            UpdateStatusBanner.Subtitle = "Checking GitHub for the latest release...";
            ClearSkipButton.IsVisible = false;
        }

        try
        {
            var result = await updateService.CheckLatestReleaseAsync(respectIgnoreSetting: !manual, checkCts.Token);
            if (checkCts.IsCancellationRequested) return;

            switch (result.Status)
            {
                case UpdateCheckStatus.UpdateAvailable when result.Version != null:
                    Update.IsVisible = true;
                    Update.Subtitle = string.Format(Locale.Update_UpdateFormat, result.Version);
                    UpdateStatusBanner.IsVisible = false;
                    break;
                case UpdateCheckStatus.Skipped when result.Version != null:
                    Update.IsVisible = true;
                    Update.Subtitle = string.Format(Locale.Update_UpdateFormat, result.Version);
                    RefreshSkippedBanner();
                    break;
                case UpdateCheckStatus.UpToDate:
                    Update.IsVisible = false;
                    if (manual)
                    {
                        UpdateStatusBanner.IsVisible = true;
                        UpdateStatusBanner.Subtitle = $"You are running the latest version ({Version}).";
                    }
                    break;
                default:
                    if (manual)
                    {
                        UpdateStatusBanner.IsVisible = true;
                        UpdateStatusBanner.Subtitle = "Could not reach GitHub releases right now. Please try again later.";
                    }
                    break;
            }
        }
        catch (Exception)
        {
            if (manual)
            {
                UpdateStatusBanner.IsVisible = true;
                UpdateStatusBanner.Subtitle = "Could not reach GitHub releases right now.";
            }
        }
        finally
        {
            if (manual)
                CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void CheckForUpdates_OnClick(object? sender, RoutedEventArgs e) =>
        _ = CheckForUpdatesAsync(manual: true);

    private void ClearSkippedVersion_OnClick(object? sender, RoutedEventArgs e)
    {
        updateService.ClearSkippedVersion();
        ClearSkipButton.IsVisible = false;
        _ = CheckForUpdatesAsync(manual: true);
    }

    private void GoToUpdate(object? sender, RoutedEventArgs e) =>
        LaunchExternal(UpdateService.LatestReleaseUrl);

    private void GoToRepository(object? sender, RoutedEventArgs e) =>
        LaunchExternal(UpdateService.RepositoryUrl);

    private void GoToIssues(object? sender, RoutedEventArgs e) =>
        LaunchExternal(UpdateService.IssuesUrl);

    private void OpenDataFolder(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.CurrentFolder);
        LaunchExternal(Const.CurrentFolder);
    }

    private void OpenLogsFolder(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Const.LogsFolder);
        LaunchExternal(Const.LogsFolder);
    }

    private static void LaunchExternal(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[About] Failed launching '{target}': {ex.Message}");
        }
    }

    private void ResetDefaults(object? sender, RoutedEventArgs e)
    {
        if (!resetConfirmPending)
        {
            resetConfirmPending = true;
            ResetDefaultsButton.Content = "Click Again to Confirm";
            return;
        }

        resetConfirmPending = false;
        var current = appSettingsProvider.Get();
        new StartupService().SetRunOnStartup(false);
        var defaults = AppSettings.CreateDefault() with { Onboarding = current.EffectiveOnboarding };
        appSettingsProvider.Save(defaults);
        ResetDefaultsButton.Content = "Defaults Restored";
        UpdateStatusBanner.IsVisible = true;
        UpdateStatusBanner.Subtitle = "Settings restored to defaults. Restart uWidgets if window frames changed.";
    }

    public void Dispose()
    {
        checkCts.Cancel();
        checkCts.Dispose();
        GC.SuppressFinalize(this);
    }
}