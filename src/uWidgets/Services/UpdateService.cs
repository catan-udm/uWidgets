using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using uWidgets.Core.Interfaces;
using uWidgets.Views;

namespace uWidgets.Services;

public class UpdateService : IDisposable
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly CancellationTokenSource disposeCts = new();
    private bool disposed;

    public UpdateService(IAppSettingsProvider appSettingsProvider)
    {
        this.appSettingsProvider = appSettingsProvider;
        TimerService.Timer1Day.Subscribe(OnDailyUpdateTick);
    }

    private void OnDailyUpdateTick()
    {
        if (appSettingsProvider.Get().CheckUpdatesAutomatically)
            CheckForUpdates();
    }

    public void CheckForUpdates()
    {
        _ = CheckForUpdatesAsync();
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            var version = await GetUpdateVersionAsync();
            if (version == null || disposed) return;

            new UpdatePopup(version, this).Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Update check failed: {ex.Message}");
        }
    }

    public async Task<Version?> GetUpdateVersionAsync(CancellationToken cancellationToken = default)
    {
        if (disposed) return null;

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disposeCts.Token);
            using var response = await httpClient.GetAsync(
                "https://github.com/creewick/uWidgets/releases/latest",
                linkedCts.Token);

            if (!response.IsSuccessStatusCode) return null;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestVersionText = response.RequestMessage?.RequestUri?.ToString().Split("/").Last().Replace("v", "") ?? "";

            if (!Version.TryParse(latestVersionText, out var latestVersion)) return null;

            var ignoreVersionText = appSettingsProvider.Get().IgnoreUpdate;

            if (ignoreVersionText != null && Version.TryParse(ignoreVersionText, out var ignoreVersion) &&
                latestVersion <= ignoreVersion)
                return null;

            if (latestVersion <= currentVersion)
                return null;

            return latestVersion;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Failed to query latest release: {ex.Message}");
            return null;
        }
    }

    public void SkipVersion(Version version)
    {
        var settings = appSettingsProvider.Get() with { IgnoreUpdate = version.ToString() };
        appSettingsProvider.Save(settings);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        TimerService.Timer1Day.Unsubscribe(OnDailyUpdateTick);
        disposeCts.Cancel();
        disposeCts.Dispose();
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}