using Avalonia.Media;
using Monitor.ViewModels;

namespace Monitor.Models;

public enum DeviceType
{
    Host,
    Headset,
    Mouse,
    Keyboard,
    Gamepad,
    Other
}

public record DeviceBatteryInfo(
    string DeviceId,
    string Name,
    DeviceType Type,
    double BatteryLevel,
    bool IsCharging,
    bool IsConnected)
{
    public int BatteryPercent => (int)Math.Clamp(Math.Round(BatteryLevel * 100), 0, 100);

    public string FormattedLevel => IsCharging ? $"{BatteryPercent}% ⚡" : $"{BatteryPercent}%";

    public StreamGeometry Icon => Type switch
    {
        DeviceType.Host => IsCharging ? MetricIcon.BatteryCharging.Value : MetricIcon.DeviceLaptop.Value,
        DeviceType.Headset => MetricIcon.DeviceHeadphones.Value,
        DeviceType.Mouse => MetricIcon.DeviceMouse.Value,
        DeviceType.Keyboard => MetricIcon.DeviceKeyboard.Value,
        DeviceType.Gamepad => MetricIcon.DeviceGamepad.Value,
        _ => MetricIcon.BatteryLevel.Value
    };
}
