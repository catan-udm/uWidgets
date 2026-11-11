using System.Collections.ObjectModel;
using Avalonia.Media;
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

    public void Update(DeviceBatteryInfo info)
    {
        DeviceId = info.DeviceId;
        Name = info.Name;
        BatteryPercent = info.BatteryPercent;
        BatteryLevel = info.BatteryLevel;
        IsCharging = info.IsCharging;
        Icon = info.Icon;
        FormattedLevel = info.FormattedLevel;

        if (info.IsCharging)
        {
            StatusBrush = new SolidColorBrush(Color.Parse("#10B981")); // Emerald
        }
        else if (info.BatteryPercent <= 15)
        {
            StatusBrush = new SolidColorBrush(Color.Parse("#EF4444")); // Red
        }
        else if (info.BatteryPercent <= 30)
        {
            StatusBrush = new SolidColorBrush(Color.Parse("#F59E0B")); // Amber
        }
        else
        {
            StatusBrush = new SolidColorBrush(Color.Parse("#10B981")); // Green
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

    private bool hasDevices;
    public bool HasDevices
    {
        get => hasDevices;
        private set => this.RaiseAndSetIfChanged(ref hasDevices, value);
    }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        var all = await PeripheralBatteryService.GetAllBatteriesAsync();
        
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
    }

    public void Dispose()
    {
        TimerService.Timer5Seconds.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
