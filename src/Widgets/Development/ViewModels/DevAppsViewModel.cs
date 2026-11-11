using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Media;
using Development.Models;
using Development.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Development.ViewModels;

public class DevAppItemViewModel : ReactiveObject
{
    private DevAppInstanceInfo info;

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
        });

        StopCommand = ReactiveCommand.Create(() => killCallback(info.ProcessId));
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

    public ICommand OpenBrowserCommand { get; }
    public ICommand StopCommand { get; }

    public void Update(DevAppInstanceInfo newInfo)
    {
        info = newInfo;
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

    public DevAppsViewModel(DevAppsModel model)
    {
        this.model = model;
        Apps = [];

        Update();
        TimerService.Timer5Seconds.Subscribe(Update);
    }

    public ObservableCollection<DevAppItemViewModel> Apps { get; }

    private bool hasApps;
    public bool HasApps
    {
        get => hasApps;
        private set => this.RaiseAndSetIfChanged(ref hasApps, value);
    }

    private string headerSummary = "Dev Apps";
    public string HeaderSummary
    {
        get => headerSummary;
        private set => this.RaiseAndSetIfChanged(ref headerSummary, value);
    }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        var list = await DevProcessDetectorService.GetActiveDevAppsAsync();
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
    }

    private void HandleStopApp(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            p.Kill(true);
        }
        catch { }

        _ = UpdateAsync();
    }

    public void Dispose()
    {
        TimerService.Timer5Seconds.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
