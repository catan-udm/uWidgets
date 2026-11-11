using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Monitor.Models;

namespace Monitor.Services;

[SupportedOSPlatform("windows10.0.10240.0")]
public static class PeripheralBatteryService
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    private const string BatteryLevelPropKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

    public static DeviceBatteryInfo? GetHostBattery()
    {
        if (!OperatingSystem.IsWindows()) return null;

        if (GetSystemPowerStatus(out var status))
        {
            // BatteryFlag 128 means No System Battery (e.g. desktop PC without UPS)
            if (status.BatteryFlag != 128 && status.BatteryLifePercent <= 100)
            {
                bool isCharging = status.ACLineStatus == 1 && (status.BatteryFlag & 8) != 0;
                double level = Math.Clamp(status.BatteryLifePercent / 100.0, 0.0, 1.0);
                return new DeviceBatteryInfo(
                    DeviceId: "host_battery",
                    Name: "PC / Laptop Battery",
                    Type: DeviceType.Host,
                    BatteryLevel: level,
                    IsCharging: isCharging,
                    IsConnected: true
                );
            }
        }

        return null;
    }

    public static async Task<List<DeviceBatteryInfo>> GetAllBatteriesAsync()
    {
        var result = new List<DeviceBatteryInfo>();

        var host = GetHostBattery();
        if (host != null)
        {
            result.Add(host);
        }

        if (!OperatingSystem.IsWindows()) return result;

        try
        {
            var peripheralBatteries = await Task.Run(GetPeripheralsAsync);
            result.AddRange(peripheralBatteries);
        }
        catch
        {
            // Fail gracefully if Bluetooth/PnP services are unavailable
        }

        return result;
    }

    private static async Task<List<DeviceBatteryInfo>> GetPeripheralsAsync()
    {
        var list = new List<DeviceBatteryInfo>();

        try
        {
            // Query devices with battery property
            var properties = new[] { "System.ItemNameDisplay", BatteryLevelPropKey };
            var selector = $"System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#True";
            
            var devices = await DeviceInformation.FindAllAsync(selector, properties);

            foreach (var d in devices)
            {
                if (d.Properties.TryGetValue(BatteryLevelPropKey, out var batteryVal) && batteryVal != null)
                {
                    if (Convert.ToInt32(batteryVal) is var pct and >= 0 and <= 100)
                    {
                        var name = d.Name;
                        if (string.IsNullOrWhiteSpace(name) && d.Properties.TryGetValue("System.ItemNameDisplay", out var dispName))
                        {
                            name = dispName?.ToString() ?? "Wireless Device";
                        }

                        list.Add(new DeviceBatteryInfo(
                            DeviceId: d.Id,
                            Name: name,
                            Type: InferDeviceType(name),
                            BatteryLevel: pct / 100.0,
                            IsCharging: false,
                            IsConnected: true
                        ));
                    }
                }
            }
        }
        catch
        {
            // Ignore device enumeration exceptions
        }

        // Secondary fallback for BLE devices exposing GATT 0x180F
        try
        {
            var bleSelector = BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);
            var bleDevices = await DeviceInformation.FindAllAsync(bleSelector);

            foreach (var devInfo in bleDevices)
            {
                // Check if already found via property bag
                if (list.Any(x => x.DeviceId == devInfo.Id || x.Name.Equals(devInfo.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    using var ble = await BluetoothLEDevice.FromIdAsync(devInfo.Id);
                    if (ble == null) continue;

                    var gattResult = await ble.GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Cached);
                    if (gattResult.Status == GattCommunicationStatus.Success && gattResult.Services.Count > 0)
                    {
                        var service = gattResult.Services[0];
                        var charResult = await service.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel, BluetoothCacheMode.Cached);
                        if (charResult.Status == GattCommunicationStatus.Success && charResult.Characteristics.Count > 0)
                        {
                            var charac = charResult.Characteristics[0];
                            var readResult = await charac.ReadValueAsync(BluetoothCacheMode.Cached);
                            if (readResult.Status == GattCommunicationStatus.Success && readResult.Value != null)
                            {
                                using var reader = DataReader.FromBuffer(readResult.Value);
                                if (reader.UnconsumedBufferLength > 0)
                                {
                                    byte b = reader.ReadByte();
                                    list.Add(new DeviceBatteryInfo(
                                        DeviceId: devInfo.Id,
                                        Name: string.IsNullOrWhiteSpace(devInfo.Name) ? "Bluetooth Device" : devInfo.Name,
                                        Type: InferDeviceType(devInfo.Name),
                                        BatteryLevel: b / 100.0,
                                        IsCharging: false,
                                        IsConnected: true
                                    ));
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore single device GATT failure
                }
            }
        }
        catch
        {
            // Ignore BLE query errors
        }

        return list;
    }

    public static DeviceType InferDeviceType(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return DeviceType.Other;

        var lower = name.ToLowerInvariant();
        if (lower.Contains("headset") || lower.Contains("headphone") || lower.Contains("earbud") ||
            lower.Contains("airpods") || lower.Contains("buds") || lower.Contains("wh-") || lower.Contains("wf-") ||
            lower.Contains("soundcore") || lower.Contains("jabra") || lower.Contains("bose") || lower.Contains("sony"))
        {
            return DeviceType.Headset;
        }

        if (lower.Contains("mouse") || lower.Contains("trackball") || lower.Contains("mx master") || lower.Contains("anywhere"))
        {
            return DeviceType.Mouse;
        }

        if (lower.Contains("keyboard") || lower.Contains("keychron") || lower.Contains("mx keys"))
        {
            return DeviceType.Keyboard;
        }

        if (lower.Contains("controller") || lower.Contains("gamepad") || lower.Contains("xbox") ||
            lower.Contains("dualsense") || lower.Contains("dualshock"))
        {
            return DeviceType.Gamepad;
        }

        return DeviceType.Other;
    }
}
