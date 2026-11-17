using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Development.Models;
using Development.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Development.ViewModels;

public class DevAppItemViewModel : ReactiveObject
{
    private DevAppInstanceInfo info;
    private DateTimeOffset confirmDeadlineUtc = DateTimeOffset.MinValue;
    private bool isConfirmingStop;

    public DevAppItemViewModel(DevAppInstanceInfo info, Action<int> killCallback)
    {
        this.info = info;

        OpenBrowserCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(info.Url) { UseShellExecute = true });
            }
            catch { }
        }, outputScheduler: RxApp.MainThreadScheduler);

        StopCommand = ReactiveCommand.Create(() =>
        {
            if (IsConfirmingStop && DateTimeOffset.UtcNow <= confirmDeadlineUtc)
            {
                IsConfirmingStop = false;
                killCallback(info.ProcessId);
                return;
            }

            confirmDeadlineUtc = DateTimeOffset.UtcNow.AddSeconds(4);
            IsConfirmingStop = true;
        }, outputScheduler: RxApp.MainThreadScheduler);
    }

    public int ProcessId => info.ProcessId;
    public string AppName => info.AppName;
    public string Framework => info.Framework;
    public int Port => info.Port;
    public long MemoryMb => info.MemoryWorkingSetMb;
    public string Url => info.Url;
    public StreamGeometry Icon => info.Icon;

    public string PortDisplay => $":{Port}";
    public string MemoryDisplay => $"{MemoryMb} MB";

    public bool IsConfirmingStop
    {
        get => isConfirmingStop;
        private set
        {
            this.RaiseAndSetIfChanged(ref isConfirmingStop, value);
            this.RaisePropertyChanged(nameof(StopTooltip));
        }
    }

    public string StopTooltip => IsConfirmingStop ? "Click again to kill process tree" : "Stop Process";

    public ICommand OpenBrowserCommand { get; }
    public ICommand StopCommand { get; }

    public void Update(DevAppInstanceInfo newInfo)
    {
        info = newInfo;
        if (isConfirmingStop && DateTimeOffset.UtcNow > confirmDeadlineUtc)
        {
            IsConfirmingStop = false;
        }
        this.RaisePropertyChanged(nameof(AppName));
        this.RaisePropertyChanged(nameof(Framework));
        this.RaisePropertyChanged(nameof(Port));
        this.RaisePropertyChanged(nameof(MemoryMb));
        this.RaisePropertyChanged(nameof(PortDisplay));
        this.RaisePropertyChanged(nameof(MemoryDisplay));
        this.RaisePropertyChanged(nameof(Icon));
    }
}

public class DevAppsViewModel : ReactiveObject, IDisposable
{
    private readonly DevAppsModel model;
    private readonly int refreshSeconds;
    private DateTimeOffset nextPollUtc = DateTimeOffset.MinValue;
    private int isUpdating;

    public DevAppsViewModel(DevAppsModel model)
    {
        this.model = model;
        refreshSeconds = Math.Clamp(model.RefreshIntervalSeconds, 1, 60);
        Apps = [];

        OnTimerTick();
        TimerService.Timer1Second.Subscribe(OnTimerTick);
    }

    public ObservableCollection<DevAppItemViewModel> Apps { get; }

    private bool hasApps;
    public bool HasApps
    {
        get => hasApps;
        private set => this.RaiseAndSetIfChanged(ref hasApps, value);
    }

    private string headerSummary = string.Empty;
    public string HeaderSummary
    {
        get => headerSummary;
        private set => this.RaiseAndSetIfChanged(ref headerSummary, value);
    }

    private void OnTimerTick()
    {
        var now = DateTimeOffset.UtcNow;
        if (now < nextPollUtc) return;
        nextPollUtc = now.AddSeconds(refreshSeconds);
        _ = UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        if (Interlocked.CompareExchange(ref isUpdating, 1, 0) != 0)
            return;

        try
        {
            var list = await DevProcessDetectorService.GetActiveDevAppsAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasApps = list.Count > 0;
                HeaderSummary = list.Count > 0 ? $"{list.Count} Active Instance{(list.Count > 1 ? "s" : "")}" : "No Active Servers";

                var toRemove = Apps.Where(a => list.All(l => l.Port != a.Port)).ToList();
                foreach (var rem in toRemove) Apps.Remove(rem);

                foreach (var item in list)
                {
                    var existing = Apps.FirstOrDefault(a => a.Port == item.Port);
                    if (existing != null)
                    {
                        existing.Update(item);
                    }
                    else
                    {
                        Apps.Add(new DevAppItemViewModel(item, HandleStopApp));
                    }
                }
            });
        }
        catch (Exception)
        {
        }
        finally
        {
            Volatile.Write(ref isUpdating, 0);
        }
    }

    private void HandleStopApp(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.Kill(true);
        }
        catch { }

        nextPollUtc = DateTimeOffset.MinValue;
        _ = UpdateAsync();
    }

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(OnTimerTick);
        GC.SuppressFinalize(this);
    }
}
