using System.Threading;
using System.Windows.Input;
using Avalonia.Threading;
using Monitor.Locales;
using Monitor.Models;
using Monitor.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Monitor.ViewModels;

public class SingleMetricViewModel : ReactiveObject, IDisposable
{
    private readonly SingleMetricModel model;
    private DateTimeOffset nextPeripheralPollUtc = DateTimeOffset.MinValue;
    private DateTimeOffset confirmDeadlineUtc = DateTimeOffset.MinValue;
    private int isUpdating;
    private bool isConfirmingEmpty;

    public SingleMetricViewModel(SingleMetricModel model)
    {
        this.model = model;
        metric = new MetricViewModel(null, MetricService.GetMetricIcon(model.Metric));

        EmptyRecycleBinCommand = ReactiveCommand.CreateFromTask(HandleEmptyRecycleBinAsync, outputScheduler: RxApp.MainThreadScheduler);

        Update();
        TimerService.Timer1Second.Subscribe(Update);
    }

    private MetricViewModel? metric;
    public MetricViewModel? Metric
    {
        get => metric;
        private set
        {
            this.RaiseAndSetIfChanged(ref metric, value);
            this.RaisePropertyChanged(nameof(ToolTipText));
            this.RaisePropertyChanged(nameof(CanShowEmptyButton));
        }
    }

    public bool IsRecycleBin => model.Metric == MetricType.RecycleBin;
    public bool CanShowEmptyButton => IsRecycleBin && (Metric?.Value ?? 0) > 0;

    public bool IsConfirmingEmpty
    {
        get => isConfirmingEmpty;
        private set
        {
            this.RaiseAndSetIfChanged(ref isConfirmingEmpty, value);
            this.RaisePropertyChanged(nameof(EmptyButtonText));
            this.RaisePropertyChanged(nameof(EmptyButtonTooltip));
        }
    }

    public string EmptyButtonText => IsConfirmingEmpty ? "Confirm?" : "Empty";
    public string EmptyButtonTooltip => IsConfirmingEmpty ? "Click again to permanently empty Recycle Bin" : "Empty Recycle Bin";
    public ICommand EmptyRecycleBinCommand { get; }

    public string ToolTipText => $"{GetMetricTitle(model.Metric)}: {Metric?.Text ?? "--"}";

    private static string GetMetricTitle(MetricType type) => type switch
    {
        MetricType.CpuUsage => Locale.Monitor_Metric_0,
        MetricType.RamUsage => Locale.Monitor_Metric_1,
        MetricType.DiskUsage => Locale.Monitor_Metric_2,
        MetricType.NetworkUsage => Locale.Monitor_Metric_3,
        MetricType.BatteryLevel => Locale.Monitor_Metric_4,
        MetricType.StorageCapacity => Locale.Monitor_Metric_5,
        MetricType.GpuUsage => Locale.Monitor_Metric_6,
        MetricType.RecycleBin => Locale.Monitor_Metric_7,
        _ => type.ToString()
    };

    private async Task HandleEmptyRecycleBinAsync()
    {
        if (!IsRecycleBin) return;

        if (!IsConfirmingEmpty || DateTimeOffset.UtcNow > confirmDeadlineUtc)
        {
            confirmDeadlineUtc = DateTimeOffset.UtcNow.AddSeconds(4);
            IsConfirmingEmpty = true;
            return;
        }

        IsConfirmingEmpty = false;
        await Task.Run(MetricService.EmptyRecycleBin);
        await UpdateAsync(forcePeripheralRefresh: true);
    }

    private void Update() => _ = UpdateAsync(forcePeripheralRefresh: false);

    private async Task UpdateAsync(bool forcePeripheralRefresh)
    {
        if (IsConfirmingEmpty && DateTimeOffset.UtcNow > confirmDeadlineUtc)
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsConfirmingEmpty = false);
        }

        bool isPeripheralBattery = model.Metric == MetricType.BatteryLevel &&
                                   !string.IsNullOrEmpty(model.TargetDeviceId) &&
                                   model.TargetDeviceId != "host_battery";

        if (isPeripheralBattery && !forcePeripheralRefresh && DateTimeOffset.UtcNow < nextPeripheralPollUtc)
            return;

        if (Interlocked.CompareExchange(ref isUpdating, 1, 0) != 0)
            return;

        try
        {
            double? value;
            var icon = MetricService.GetMetricIcon(model.Metric);

            if (isPeripheralBattery)
            {
                nextPeripheralPollUtc = DateTimeOffset.UtcNow.AddSeconds(5);
                var all = await PeripheralBatteryService.GetAllBatteriesAsync(onlyConnected: true);
                var dev = all.FirstOrDefault(d => d.DeviceId == model.TargetDeviceId);
                value = dev?.BatteryLevel;
                if (dev != null)
                {
                    icon = dev.Icon;
                }
            }
            else
            {
                value = await MetricService.GetMetricValue(model.Metric, model.TargetDeviceId);
                if (model.Metric == MetricType.BatteryLevel)
                {
                    var host = PeripheralBatteryService.GetHostBattery();
                    if (host != null && host.IsCharging)
                    {
                        icon = MetricIcon.BatteryCharging.Value;
                    }
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Metric = new MetricViewModel(value.HasValue ? Math.Clamp(value.Value, 0d, 1d) : null, icon);
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

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
