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

    private static readonly HashSet<string> GenericNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bluetooth Device",
        "Wireless Device",
        "Wireless Accessory",
        "HID-compliant mouse",
        "HID-compliant device",
        "HID-compliant system controller",
        "HID Keyboard Device",
        "Standard PS/2 Keyboard",
        "Bluetooth Audio Device",
        "Generic Bluetooth Adapter",
        "USB Input Device",
        "System Product Name"
    };

    private static string? cachedHostName;

    public static string GetHostDeviceName()
    {
        if (cachedHostName != null) return cachedHostName;

        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            if (key != null)
            {
                var prodName = key.GetValue("SystemProductName") as string;
                if (!string.IsNullOrWhiteSpace(prodName) && !prodName.Equals("System Product Name", StringComparison.OrdinalIgnoreCase))
                {
                    cachedHostName = prodName.Trim();
                    return cachedHostName;
                }
            }
        }
        catch { }

        cachedHostName = Environment.MachineName;
        return cachedHostName;
    }

    public static bool IsGenericName(string? name) =>
        string.IsNullOrWhiteSpace(name) || GenericNames.Contains(name.Trim());

    private static readonly string[] AccessoryNameSuffixes =
    {
        " Hands-Free AG",
        " Hands-Free",
        " Avrcp Transport",
        " Bluetooth Audio",
        " Stereo"
    };

    public static string CleanDeviceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        foreach (var s in AccessoryNameSuffixes)
        {
            if (name.EndsWith(s, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - s.Length).Trim();
            }
        }
        return name;
    }

    public static string ResolveAccessoryModelName(DeviceInformation d, string? fallbackManufacturer = null)
    {
        string? model = null;

        if (d.Properties.TryGetValue("System.Devices.ModelName", out var m) && m is string ms && !IsGenericName(ms))
        {
            model = ms.Trim();
        }
        else if (d.Properties.TryGetValue("System.ItemNameDisplay", out var ind) && ind is string inds && !IsGenericName(inds))
        {
            model = inds.Trim();
        }
        else if (d.Properties.TryGetValue("System.Devices.FriendlyName", out var fn) && fn is string fns && !IsGenericName(fns))
        {
            model = fns.Trim();
        }
        else if (!IsGenericName(d.Name))
        {
            model = d.Name.Trim();
        }

        string? manufacturer = fallbackManufacturer;
        if (d.Properties.TryGetValue("System.Devices.DeviceManufacturer", out var mfg) && mfg is string mfgs && !string.IsNullOrWhiteSpace(mfgs))
        {
            var cleanMfg = mfgs.Trim();
            if (!IsGenericName(cleanMfg) && !cleanMfg.Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
            {
                manufacturer = cleanMfg;
            }
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            if (!string.IsNullOrWhiteSpace(manufacturer) && !model.Contains(manufacturer, StringComparison.OrdinalIgnoreCase))
            {
                return $"{manufacturer} {model}";
            }
            return model;
        }

        return "Wireless Accessory";
    }

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
                    Name: GetHostDeviceName(),
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

    private static readonly Guid ServiceUuidDeviceInformation = new("0000180A-0000-1000-8000-00805F9B34FB");
    private static readonly Guid CharUuidModelNumber = new("00002A24-0000-1000-8000-00805F9B34FB");
    private static readonly Guid CharUuidManufacturer = new("00002A29-0000-1000-8000-00805F9B34FB");

    private static async Task<List<DeviceBatteryInfo>> GetPeripheralsAsync()
    {
        var list = new List<DeviceBatteryInfo>();
        var seenContainers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Query present devices with battery property and model/friendly identification
            var properties = new[]
            {
                "System.ItemNameDisplay",
                "System.Devices.ModelName",
                "System.Devices.FriendlyName",
                "System.Devices.DeviceManufacturer",
                "System.Devices.ContainerId",
                BatteryLevelPropKey
            };
            var selector = "System.Devices.Present:=System.StructuredQueryType.Boolean#True";
            
            var devices = await DeviceInformation.FindAllAsync(selector, properties, DeviceInformationKind.Device);

            foreach (var d in devices)
            {
                if (d.Properties.TryGetValue(BatteryLevelPropKey, out var batteryVal) && batteryVal != null)
                {
                    if (Convert.ToInt32(batteryVal) is var pct and >= 0 and <= 100)
                    {
                        string name = ResolveAccessoryModelName(d);
                        string containerKey = d.Id;

                        if (d.Properties.TryGetValue("System.Devices.ContainerId", out var cIdObj) && cIdObj is Guid cGuid)
                        {
                            containerKey = cGuid.ToString();
                            if (!seenContainers.Add(containerKey))
                            {
                                continue;
                            }

                            try
                            {
                                var container = await DeviceInformation.CreateFromIdAsync(
                                    "{" + cGuid.ToString() + "}",
                                    new[] { "System.ItemNameDisplay", "System.Devices.FriendlyName", "System.Devices.ModelName" },
                                    DeviceInformationKind.DeviceContainer);

                                if (container != null && !IsGenericName(container.Name))
                                {
                                    name = container.Name;
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            if (!seenContainers.Add(containerKey))
                            {
                                continue;
                            }
                        }

                        name = CleanDeviceName(name);

                        list.Add(new DeviceBatteryInfo(
                            DeviceId: d.Id,
                            Name: name,
                            Type: InferDeviceType(name, d.Id),
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
                    IReadOnlyList<GattDeviceService>? services = null;
                    try
                    {
                        services = gattResult.Services;
                    }
                    catch { }

                    if (services != null)
                    {
                        try
                        {
                            if (gattResult.Status == GattCommunicationStatus.Success && services.Count > 0)
                            {
                                var service = services[0];
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
                                            var resolvedName = await ResolveBleModelNameAsync(ble, devInfo.Name);
                                            resolvedName = CleanDeviceName(resolvedName);

                                            list.Add(new DeviceBatteryInfo(
                                                DeviceId: devInfo.Id,
                                                Name: resolvedName,
                                                Type: InferDeviceType(resolvedName, devInfo.Id),
                                                BatteryLevel: b / 100.0,
                                                IsCharging: false,
                                                IsConnected: true
                                            ));
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            foreach (var s in services)
                            {
                                try { s?.Dispose(); } catch { }
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

    private static async Task<string> ResolveBleModelNameAsync(BluetoothLEDevice ble, string initialName)
    {
        if (!IsGenericName(initialName)) return initialName;

        try
        {
            var disResult = await ble.GetGattServicesForUuidAsync(ServiceUuidDeviceInformation, BluetoothCacheMode.Cached);
            if (disResult.Status == GattCommunicationStatus.Success && disResult.Services.Count > 0)
            {
                var dis = disResult.Services[0];
                try
                {
                    string? model = null;
                    string? mfg = null;

                    var modelCharResult = await dis.GetCharacteristicsForUuidAsync(CharUuidModelNumber, BluetoothCacheMode.Cached);
                    if (modelCharResult.Status == GattCommunicationStatus.Success && modelCharResult.Characteristics.Count > 0)
                    {
                        var read = await modelCharResult.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Cached);
                        if (read.Status == GattCommunicationStatus.Success && read.Value != null)
                        {
                            using var reader = DataReader.FromBuffer(read.Value);
                            model = reader.ReadString(reader.UnconsumedBufferLength)?.Trim();
                        }
                    }

                    var mfgCharResult = await dis.GetCharacteristicsForUuidAsync(CharUuidManufacturer, BluetoothCacheMode.Cached);
                    if (mfgCharResult.Status == GattCommunicationStatus.Success && mfgCharResult.Characteristics.Count > 0)
                    {
                        var read = await mfgCharResult.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Cached);
                        if (read.Status == GattCommunicationStatus.Success && read.Value != null)
                        {
                            using var reader = DataReader.FromBuffer(read.Value);
                            mfg = reader.ReadString(reader.UnconsumedBufferLength)?.Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(model) && !IsGenericName(model))
                    {
                        if (!string.IsNullOrWhiteSpace(mfg) && !model.Contains(mfg, StringComparison.OrdinalIgnoreCase))
                        {
                            return $"{mfg} {model}";
                        }
                        return model;
                    }
                }
                finally
                {
                    foreach (var s in disResult.Services) { try { s?.Dispose(); } catch { } }
                }
            }
        }
        catch { }

        if (!IsGenericName(ble.DeviceInformation?.Name))
        {
            return ble.DeviceInformation!.Name.Trim();
        }

        return "Wireless Accessory";
    }

    public static DeviceType InferDeviceType(string? name, string? deviceId = null)
    {
        if (!string.IsNullOrEmpty(deviceId))
        {
            var lowerId = deviceId.ToLowerInvariant();
            // Hands-Free Profile (0000111e), Headset Profile (00001108), A2DP Audio Sink (0000110b)
            if (lowerId.Contains("0000111e") || lowerId.Contains("00001108") || lowerId.Contains("0000110b"))
            {
                return DeviceType.Headset;
            }
        }

        if (string.IsNullOrWhiteSpace(name)) return DeviceType.Other;

        var lower = name.ToLowerInvariant();
        if (lower.Contains("headset") || lower.Contains("headphone") || lower.Contains("earbud") ||
            lower.Contains("airpods") || lower.Contains("buds") || lower.Contains("wh-") || lower.Contains("wf-") ||
            lower.Contains("soundcore") || lower.Contains("jabra") || lower.Contains("bose") || lower.Contains("sony") ||
            lower.Contains("hands-free") || lower.Contains("handsfree") || lower.Contains("earphone") ||
            lower.Contains("audio") || lower.Contains("studio") || lower.Contains("ugreen") || lower.Contains("sennheiser") ||
            lower.Contains("shokz") || lower.Contains("jbl") || lower.Contains("beats") || lower.Contains("anker"))
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
