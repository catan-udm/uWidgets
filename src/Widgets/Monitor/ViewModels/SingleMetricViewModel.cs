using Monitor.Models;
using Monitor.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Monitor.ViewModels;

public class SingleMetricViewModel : ReactiveObject
{
    private readonly SingleMetricModel model;

    public SingleMetricViewModel(SingleMetricModel model)
    {
        this.model = model;
        metric = new MetricViewModel(0d);
        TimerService.Timer1Second.Subscribe(Update);
    }
    
    private MetricViewModel? metric;
    public MetricViewModel? Metric 
    {
        get => metric;
        private set => this.RaiseAndSetIfChanged(ref metric, value);
    }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        var value = await MetricService.GetMetricValue(model.Metric, model.TargetDeviceId);
        var icon = MetricService.GetMetricIcon(model.Metric);

        if (model.Metric == MetricType.BatteryLevel)
        {
            if (string.IsNullOrEmpty(model.TargetDeviceId) || model.TargetDeviceId == "host_battery")
            {
                var host = PeripheralBatteryService.GetHostBattery();
                if (host != null && host.IsCharging)
                {
                    icon = MetricIcon.BatteryCharging.Value;
                }
            }
            else
            {
                var all = await PeripheralBatteryService.GetAllBatteriesAsync();
                var dev = all.FirstOrDefault(d => d.DeviceId == model.TargetDeviceId);
                if (dev != null)
                {
                    icon = dev.Icon;
                }
            }
        }

        if (value.HasValue)
            Metric = new MetricViewModel(value.Value, icon);
    }
}
