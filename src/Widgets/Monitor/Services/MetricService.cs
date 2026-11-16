using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Monitor.Models;
using Monitor.ViewModels;

namespace Monitor.Services;

public static class MetricService
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    public const uint SHERB_NOCONFIRMATION = 0x00000001;
    public const uint SHERB_NOPROGRESSUI   = 0x00000002;
    public const uint SHERB_NOSOUND        = 0x00000004;

    public static StreamGeometry GetMetricIcon(MetricType type)
    {
        return type switch
        {
            MetricType.CpuUsage => MetricIcon.CpuUsage.Value,
            MetricType.RamUsage => MetricIcon.RamUsage.Value,
            MetricType.DiskUsage => MetricIcon.DiskUsage.Value,
            MetricType.NetworkUsage => MetricIcon.NetworkUsage.Value,
            MetricType.BatteryLevel => MetricIcon.BatteryLevel.Value,
            MetricType.StorageCapacity => MetricIcon.StorageCapacity.Value,
            MetricType.GpuUsage => MetricIcon.GpuUsage.Value,
            MetricType.RecycleBin => MetricIcon.RecycleBin.Value,
            _ => new StreamGeometry()
        };
    }

    public static Task<double?> GetMetricValue(MetricType type, string? targetDeviceId = null)
    {
        try
        {
            return type switch
            {
                MetricType.CpuUsage => Task.Run(GetCpuUsage),
                MetricType.RamUsage => Task.Run(GetRamUsage),
                MetricType.DiskUsage => Task.Run(GetDiskUsage),
                MetricType.NetworkUsage => Task.Run(GetNetworkUsage),
                MetricType.BatteryLevel => GetBatteryValueAsync(targetDeviceId),
                MetricType.StorageCapacity => Task.Run(GetStorageCapacity),
                MetricType.GpuUsage => Task.Run(GetGpuUsage),
                MetricType.RecycleBin => Task.Run(GetRecycleBinUsage),
                _ => throw new ArgumentException()
            };
        }
        catch (Exception)
        {
            return Task.FromResult((double?)null);
        }
    }

    private static async Task<double?> GetBatteryValueAsync(string? targetDeviceId)
    {
        if (string.IsNullOrEmpty(targetDeviceId) || targetDeviceId == "host_battery")
        {
            var host = PeripheralBatteryService.GetHostBattery();
            if (host != null) return host.BatteryLevel;
            return GetBatteryLevelWmi();
        }

        var all = await PeripheralBatteryService.GetAllBatteriesAsync();
        var match = all.FirstOrDefault(d => d.DeviceId == targetDeviceId);
        return match?.BatteryLevel;
    }

    private static double? GetCpuUsage()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor where Name='_Total'");
            using var results = searcher.Get();
            foreach (var o in results)
            {
                using var obj = (ManagementObject)o;
                return Convert.ToDouble(obj["PercentProcessorTime"]) / 100;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private static double? GetRamUsage()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            using var results = searcher.Get();
            foreach (var o in results)
            {
                using var obj = (ManagementObject)o;
                var totalVisibleMemory = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                var freePhysicalMemory = Convert.ToDouble(obj["FreePhysicalMemory"]);

                if (!(totalVisibleMemory > 0)) continue;

                var usedMemory = totalVisibleMemory - freePhysicalMemory;
                return usedMemory / totalVisibleMemory;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private static double? GetDiskUsage()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfDisk_LogicalDisk where Name='_Total'");
            using var results = searcher.Get();
            foreach (var o in results)
            {
                using var obj = (ManagementObject)o;
                return Convert.ToDouble(obj["PercentDiskTime"]) / 100;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private static double? GetStorageCapacity()
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                .ToList();

            if (drives.Count == 0) return null;

            long totalBytes = drives.Sum(d => d.TotalSize);
            long freeBytes = drives.Sum(d => d.TotalFreeSpace);

            if (totalBytes <= 0) return null;

            long usedBytes = totalBytes - freeBytes;
            return Math.Clamp((double)usedBytes / totalBytes, 0.0, 1.0);
        }
        catch
        {
            return null;
        }
    }

    private static double? GetGpuUsage()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher("select Name, UtilizationPercentage from Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine where Name like '%engtype_3D%'");
            using var results = searcher.Get();
            var luidTotals = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var o in results)
            {
                using var obj = (ManagementObject)o;
                var name = obj["Name"]?.ToString() ?? string.Empty;
                var util = Convert.ToDouble(obj["UtilizationPercentage"]);

                var luidKey = ExtractGpuLuid(name);
                luidTotals[luidKey] = luidTotals.GetValueOrDefault(luidKey) + util;
            }

            if (luidTotals.Count > 0)
            {
                var maxUsage = luidTotals.Values.Max();
                return Math.Clamp(maxUsage / 100.0, 0.0, 1.0);
            }
        }
        catch
        {
            // Fail safely if counters are uninitialized
        }

        return null;
    }

    private static string ExtractGpuLuid(string instanceName)
    {
        var idx = instanceName.IndexOf("luid_", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return "default";
        var physIdx = instanceName.IndexOf("_phys_", idx, StringComparison.OrdinalIgnoreCase);
        return physIdx > idx ? instanceName[idx..physIdx] : instanceName[idx..];
    }

    private static double? GetRecycleBinUsage()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            int hresult = SHQueryRecycleBin(null, ref info);
            if (hresult == 0)
            {
                if (info.i64NumItems == 0) return 0.0;
                // Scale 0..1 based on size (e.g. 5GB = 100% full visual, min 10% if items exist)
                double gb = info.i64Size / (1024.0 * 1024 * 1024);
                double ratio = Math.Clamp(0.1 + (gb / 5.0) * 0.9, 0.1, 1.0);
                return ratio;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    public static bool EmptyRecycleBin()
    {
        if (!OperatingSystem.IsWindows()) return false;

        try
        {
            int hr = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            return hr == 0;
        }
        catch
        {
            return false;
        }
    }

    private static double? GetBatteryLevelWmi()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher("select EstimatedChargeRemaining from Win32_Battery");
            using var results = searcher.Get();
            foreach (var o in results)
            {
                using var obj = (ManagementObject)o;
                return Convert.ToInt32(obj["EstimatedChargeRemaining"]) / 100d;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private static double? GetNetworkUsage()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            double maxUtilization = 0;
            bool foundInterface = false;

            using var searcher = new ManagementObjectSearcher("select Name, BytesTotalPersec, CurrentBandwidth from Win32_PerfFormattedData_Tcpip_NetworkInterface");
            using var results = searcher.Get();
            foreach (var o in results)
            {
                using var obj = (ManagementObject)o;
                var name = obj["Name"]?.ToString() ?? string.Empty;
                if (name.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("isatap", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Teredo", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var bandwidthBitsPerSec = Convert.ToDouble(obj["CurrentBandwidth"]);
                if (bandwidthBitsPerSec <= 0) continue;

                var bytesPerSec = Convert.ToDouble(obj["BytesTotalPersec"]);
                var utilization = Math.Clamp((bytesPerSec * 8.0) / bandwidthBitsPerSec, 0.0, 1.0);
                foundInterface = true;
                if (utilization > maxUtilization)
                {
                    maxUtilization = utilization;
                }
            }

            return foundInterface ? maxUtilization : null;
        }
        catch
        {
            // Ignore
        }

        return null;
    }
}
