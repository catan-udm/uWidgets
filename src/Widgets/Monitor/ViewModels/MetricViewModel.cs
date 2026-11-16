using Avalonia.Media;

namespace Monitor.ViewModels;

public record MetricViewModel(double? Value, StreamGeometry? Icon = null)
{
    private double ClampedValue => Math.Clamp(Value ?? 0d, 0d, 1d);
    public double StrokeDashOffset => 31.4 * (1 - ClampedValue);
    public string Text => Value.HasValue ? $"{ClampedValue:P0}" : "--";
}