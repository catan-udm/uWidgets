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

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    Skipped,
    Failed
}

public record UpdateCheckResult(UpdateCheckStatus Status, Version? Version = null);

public class UpdateService : IDisposable
{
    public const string RepositoryUrl = "https://github.com/catan-udm/uWidgets";
    public const string IssuesUrl = "https://github.com/catan-udm/uWidgets/issues";
    public const string LatestReleaseUrl = "https://github.com/catan-udm/uWidgets/releases/latest";

    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly CancellationTokenSource disposeCts = new();
    private UpdatePopup? activePopup;
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

            ShowPopup(version);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Update check failed: {ex.Message}");
        }
    }

    private void ShowPopup(Version version)
    {
        if (activePopup != null)
        {
            activePopup.Activate();
            return;
        }

        var popup = new UpdatePopup(version, this);
        activePopup = popup;
        popup.Closed += (_, _) =>
        {
            if (ReferenceEquals(activePopup, popup))
                activePopup = null;
        };
        popup.Show();
    }

    public async Task<Version?> GetUpdateVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckLatestReleaseAsync(respectIgnoreSetting: true, cancellationToken);
        return result.Status == UpdateCheckStatus.UpdateAvailable ? result.Version : null;
    }

    public async Task<UpdateCheckResult> CheckLatestReleaseAsync(bool respectIgnoreSetting = true, CancellationToken cancellationToken = default)
    {
        if (disposed) return new(UpdateCheckStatus.Failed);

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disposeCts.Token);
            using var response = await httpClient.GetAsync(LatestReleaseUrl, linkedCts.Token);

            if (!response.IsSuccessStatusCode)
                return new(UpdateCheckStatus.Failed);

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestVersionText = response.RequestMessage?.RequestUri?.ToString().Split('/').Last().Replace("v", "") ?? "";

            if (!Version.TryParse(latestVersionText, out var latestVersion))
                return new(UpdateCheckStatus.Failed);

            if (latestVersion <= currentVersion)
                return new(UpdateCheckStatus.UpToDate, latestVersion);

            var ignoreVersionText = appSettingsProvider.Get().IgnoreUpdate;
            if (respectIgnoreSetting &&
                ignoreVersionText != null &&
                Version.TryParse(ignoreVersionText, out var ignoreVersion) &&
                latestVersion <= ignoreVersion)
            {
                return new(UpdateCheckStatus.Skipped, latestVersion);
            }

            return new(UpdateCheckStatus.UpdateAvailable, latestVersion);
        }
        catch (OperationCanceledException)
        {
            return new(UpdateCheckStatus.Failed);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Failed to query latest release: {ex.Message}");
            return new(UpdateCheckStatus.Failed);
        }
    }

    public void SkipVersion(Version version)
    {
        var settings = appSettingsProvider.Get() with { IgnoreUpdate = version.ToString() };
        appSettingsProvider.Save(settings);
    }

    public void ClearSkippedVersion()
    {
        var settings = appSettingsProvider.Get() with { IgnoreUpdate = null };
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