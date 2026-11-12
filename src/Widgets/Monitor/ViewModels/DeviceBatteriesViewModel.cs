using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Monitor.Models;
using Monitor.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Monitor.ViewModels;

public class DeviceBatteryItemViewModel : ReactiveObject
{
    public DeviceBatteryItemViewModel(DeviceBatteryInfo info)
    {
        Update(info);
    }

    public string DeviceId { get; private set; } = string.Empty;

    private string name = string.Empty;
    public string Name
    {
        get => name;
        private set => this.RaiseAndSetIfChanged(ref name, value);
    }

    private int batteryPercent;
    public int BatteryPercent
    {
        get => batteryPercent;
        private set => this.RaiseAndSetIfChanged(ref batteryPercent, value);
    }

    private double batteryLevel;
    public double BatteryLevel
    {
        get => batteryLevel;
        private set => this.RaiseAndSetIfChanged(ref batteryLevel, value);
    }

    private bool isCharging;
    public bool IsCharging
    {
        get => isCharging;
        private set => this.RaiseAndSetIfChanged(ref isCharging, value);
    }

    private StreamGeometry? icon;
    public StreamGeometry? Icon
    {
        get => icon;
        private set => this.RaiseAndSetIfChanged(ref icon, value);
    }

    private string formattedLevel = string.Empty;
    public string FormattedLevel
    {
        get => formattedLevel;
        private set => this.RaiseAndSetIfChanged(ref formattedLevel, value);
    }

    private IBrush statusBrush = Brushes.Green;
    public IBrush StatusBrush
    {
        get => statusBrush;
        private set => this.RaiseAndSetIfChanged(ref statusBrush, value);
    }

    public double StrokeDashOffset => 27.6 * (1.0 - Math.Clamp(BatteryLevel, 0.0, 1.0));
    public string TooltipText => $"{Name} — {BatteryPercent}%{(IsCharging ? " (Charging)" : "")}";

    private static readonly IBrush NormalLight = new SolidColorBrush(Color.Parse("#047857"));
    private static readonly IBrush NormalDark = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush LowLight = new SolidColorBrush(Color.Parse("#B45309"));
    private static readonly IBrush LowDark = new SolidColorBrush(Color.Parse("#FBBF24"));
    private static readonly IBrush CritLight = new SolidColorBrush(Color.Parse("#B91C1C"));
    private static readonly IBrush CritDark = new SolidColorBrush(Color.Parse("#F87171"));

    public void Update(DeviceBatteryInfo info)
    {
        DeviceId = info.DeviceId;
        Name = info.Name;
        BatteryPercent = info.BatteryPercent;
        BatteryLevel = info.BatteryLevel;
        IsCharging = info.IsCharging;
        Icon = info.Icon;
        FormattedLevel = info.FormattedLevel;
        this.RaisePropertyChanged(nameof(StrokeDashOffset));
        this.RaisePropertyChanged(nameof(TooltipText));

        var isDark = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        if (info.IsCharging)
        {
            StatusBrush = isDark ? NormalDark : NormalLight;
        }
        else if (info.BatteryPercent <= 15)
        {
            StatusBrush = isDark ? CritDark : CritLight;
        }
        else if (info.BatteryPercent <= 30)
        {
            StatusBrush = isDark ? LowDark : LowLight;
        }
        else
        {
            StatusBrush = isDark ? NormalDark : NormalLight;
        }
    }
}

public class DeviceBatteriesViewModel : ReactiveObject, IDisposable
{
    private readonly DeviceBatteriesModel model;

    public DeviceBatteriesViewModel(DeviceBatteriesModel model)
    {
        this.model = model;
        Devices = [];

        Update();
        TimerService.Timer5Seconds.Subscribe(Update);
    }

    public ObservableCollection<DeviceBatteryItemViewModel> Devices { get; }
    public IEnumerable<DeviceBatteryItemViewModel> SmallDevices => Devices.Take(4);

    private bool isSmallView;
    public bool IsSmallView
    {
        get => isSmallView;
        set => this.RaiseAndSetIfChanged(ref isSmallView, value);
    }

    private bool hasDevices;
    public bool HasDevices
    {
        get => hasDevices;
        private set => this.RaiseAndSetIfChanged(ref hasDevices, value);
    }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        try
        {
            var all = await PeripheralBatteryService.GetAllBatteriesAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Remove devices that are no longer present
                var toRemove = Devices.Where(d => all.All(a => a.DeviceId != d.DeviceId)).ToList();
                foreach (var rem in toRemove)
                {
                    Devices.Remove(rem);
                }

                // Add or update current devices
                foreach (var info in all)
                {
                    var existing = Devices.FirstOrDefault(d => d.DeviceId == info.DeviceId);
                    if (existing != null)
                    {
                        existing.Update(info);
                    }
                    else
                    {
                        Devices.Add(new DeviceBatteryItemViewModel(info));
                    }
                }

                HasDevices = Devices.Count > 0;
                this.RaisePropertyChanged(nameof(SmallDevices));
            });
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        TimerService.Timer5Seconds.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
