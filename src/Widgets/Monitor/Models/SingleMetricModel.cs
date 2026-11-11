namespace Monitor.Models;

public record SingleMetricModel(MetricType Metric, string? TargetDeviceId = null);