namespace Monitor.Models;

public record DashboardModel(
    MetricType FourthMetric = MetricType.NetworkUsage,
    bool ShowLabels = true
);
