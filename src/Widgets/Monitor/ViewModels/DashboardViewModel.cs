using System;
using Avalonia.Threading;
using Monitor.Locales;
using Monitor.Models;
using Monitor.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Monitor.ViewModels;

public class DashboardItemViewModel : ReactiveObject
{
    private readonly MetricType type;
    private MetricViewModel metric;

    public DashboardItemViewModel(MetricType type, string label, bool showLabel)
    {
        this.type = type;
        Label = label;
        ShowLabel = showLabel;
        metric = new MetricViewModel(0d, MetricService.GetMetricIcon(type));
    }

    public string Label { get; }
    public bool ShowLabel { get; }

    public MetricViewModel Metric
    {
        get => metric;
        private set
        {
            this.RaiseAndSetIfChanged(ref metric, value);
            this.RaisePropertyChanged(nameof(SummaryText));
        }
    }

    public string SummaryText => ShowLabel ? $"{Label} {Metric.Text}" : Metric.Text;

    public async Task RefreshAsync()
    {
        try
        {
            var value = await MetricService.GetMetricValue(type);
            var icon = MetricService.GetMetricIcon(type);
            if (value.HasValue)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Metric = new MetricViewModel(Math.Clamp(value.Value, 0d, 1d), icon);
                });
            }
        }
        catch (Exception)
        {
        }
    }
}

public class DashboardViewModel : ReactiveObject, IDisposable
{
    public DashboardViewModel(DashboardModel model)
    {
        var fourthLabel = model.FourthMetric == MetricType.BatteryLevel
            ? Locale.Monitor_Metric_4
            : Locale.Monitor_Metric_3;

        Items =
        [
            new DashboardItemViewModel(MetricType.CpuUsage, Locale.Monitor_Metric_0, model.ShowLabels),
            new DashboardItemViewModel(MetricType.RamUsage, Locale.Monitor_Metric_1, model.ShowLabels),
            new DashboardItemViewModel(MetricType.DiskUsage, Locale.Monitor_Metric_2, model.ShowLabels),
            new DashboardItemViewModel(model.FourthMetric, fourthLabel, model.ShowLabels)
        ];

        Update();
        TimerService.Timer1Second.Subscribe(Update);
    }

    public DashboardItemViewModel[] Items { get; }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        try
        {
            await Task.WhenAll(Items.Select(x => x.RefreshAsync()));
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
