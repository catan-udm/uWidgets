using System.Text.Json;
using Monitor.Locales;
using Monitor.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Monitor.ViewModels;

public class DashboardSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private DashboardModel model = widgetLayoutProvider.Get().GetModel<DashboardModel>() ?? new DashboardModel();

    public MetricTypeViewModel[] FourthMetricTypes =>
    [
        new MetricTypeViewModel(MetricType.NetworkUsage, Locale.Monitor_Metric_3),
        new MetricTypeViewModel(MetricType.BatteryLevel, Locale.Monitor_Metric_4)
    ];

    public MetricTypeViewModel SelectedFourthMetric
    {
        get => FourthMetricTypes.FirstOrDefault(x => x.Type == model.FourthMetric) ?? FourthMetricTypes[0];
        set => Save(model with { FourthMetric = value.Type });
    }

    public bool ShowLabels
    {
        get => model.ShowLabels;
        set => Save(model with { ShowLabels = value });
    }

    private void Save(DashboardModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
