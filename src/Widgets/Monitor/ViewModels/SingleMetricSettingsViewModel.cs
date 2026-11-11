using System.Collections.ObjectModel;
using System.Text.Json;
using Monitor.Locales;
using Monitor.Models;
using Monitor.Services;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Monitor.ViewModels;

public record DeviceOptionViewModel(string Id, string Title);

public class SingleMetricSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private SingleMetricModel model;

    public SingleMetricSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<SingleMetricModel>() ?? new SingleMetricModel(MetricType.CpuUsage);

        AvailableDevices =
        [
            new DeviceOptionViewModel("host_battery", "PC / Laptop Battery (Default)")
        ];

        LoadDevicesAsync();
    }

    private async void LoadDevicesAsync()
    {
        try
        {
            var devices = await PeripheralBatteryService.GetAllBatteriesAsync();
            foreach (var dev in devices)
            {
                if (dev.DeviceId != "host_battery" && AvailableDevices.All(x => x.Id != dev.DeviceId))
                {
                    AvailableDevices.Add(new DeviceOptionViewModel(dev.DeviceId, $"{dev.Name} ({dev.BatteryPercent}%)"));
                }
            }

            this.RaisePropertyChanged(nameof(SelectedDevice));
        }
        catch
        {
            // Ignore
        }
    }

    public MetricTypeViewModel[] AllTypes =>
    [
        new MetricTypeViewModel(MetricType.CpuUsage, Locale.Monitor_Metric_0),
        new MetricTypeViewModel(MetricType.RamUsage, Locale.Monitor_Metric_1),
        new MetricTypeViewModel(MetricType.DiskUsage, Locale.Monitor_Metric_2),
        new MetricTypeViewModel(MetricType.NetworkUsage, Locale.Monitor_Metric_3),
        new MetricTypeViewModel(MetricType.BatteryLevel, Locale.Monitor_Metric_4),
        new MetricTypeViewModel(MetricType.StorageCapacity, Locale.Monitor_Metric_5),
        new MetricTypeViewModel(MetricType.GpuUsage, Locale.Monitor_Metric_6),
        new MetricTypeViewModel(MetricType.RecycleBin, Locale.Monitor_Metric_7),
    ];

    public MetricTypeViewModel SelectedType
    {
        get => AllTypes.FirstOrDefault(x => x.Type == model.Metric) ?? AllTypes[0];
        set
        {
            model = model with { Metric = value.Type };
            SaveModel();
            this.RaisePropertyChanged(nameof(SelectedType));
            this.RaisePropertyChanged(nameof(IsBatterySelected));
        }
    }

    public bool IsBatterySelected => SelectedType?.Type == MetricType.BatteryLevel;

    public ObservableCollection<DeviceOptionViewModel> AvailableDevices { get; }

    public DeviceOptionViewModel? SelectedDevice
    {
        get => AvailableDevices.FirstOrDefault(x => x.Id == (model.TargetDeviceId ?? "host_battery")) ?? AvailableDevices.FirstOrDefault();
        set
        {
            if (value == null) return;
            model = model with { TargetDeviceId = value.Id };
            SaveModel();
            this.RaisePropertyChanged(nameof(SelectedDevice));
        }
    }

    private void SaveModel()
    {
        if (widgetLayoutProvider == null) return;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
