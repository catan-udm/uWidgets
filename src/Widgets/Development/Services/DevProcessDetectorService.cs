using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Development.Models;

namespace Development.Services;

public static class DevProcessDetectorService
{
    private const int AF_INET = 2;
    private const int TCP_TABLE_OWNER_PID_ALL = 5;
    private const int MIB_TCP_STATE_LISTEN = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint remoteAddr;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public int owningPid;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwOutBufLen,
        bool bOrder,
        int ulAf,
        int tableClass,
        uint reserved = 0);

    private static readonly HashSet<int> KnownDevPorts =
    [
        3000, 3001, 3002, 3003, 5173, 5174, 8080, 8000, 4200, 5000, 3333, 8081
    ];

    public static async Task<List<DevAppInstanceInfo>> GetActiveDevAppsAsync()
    {
        if (!OperatingSystem.IsWindows())
            return [];

        return await Task.Run(() =>
        {
            var results = new List<DevAppInstanceInfo>();

            try
            {
                var listeningPorts = GetListeningPorts();
                if (listeningPorts.Count == 0) return results;

                // Inspect candidate processes
                foreach (var (port, pid) in listeningPorts)
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        var procName = proc.ProcessName.ToLowerInvariant();

                        // Target node, bun, deno, or processes running on known dev ports
                        bool isNodeOrRuntime = procName.Contains("node") || procName.Contains("bun") || procName.Contains("deno");
                        bool isDevPort = KnownDevPorts.Contains(port);

                        if (!isNodeOrRuntime && !isDevPort)
                            continue;

                        long memoryMb = 0;
                        try { memoryMb = proc.WorkingSet64 / (1024 * 1024); } catch { }

                        string cmdLine = GetCommandLine(pid);
                        var (framework, appName) = IdentifyFrameworkAndApp(procName, cmdLine);

                        results.Add(new DevAppInstanceInfo(
                            ProcessId: pid,
                            AppName: appName,
                            Framework: framework,
                            Port: port,
                            MemoryWorkingSetMb: memoryMb,
                            Url: $"http://localhost:{port}"
                        ));
                    }
                    catch
                    {
                        // Process may have exited
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return results.GroupBy(x => x.Port).Select(g => g.First()).ToList();
        });
    }

    private static List<(int Port, int Pid)> GetListeningPorts()
    {
        var list = new List<(int Port, int Pid)>();
        int bufferSize = 0;

        _ = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL);
        if (bufferSize <= 0) return list;

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            uint ret = GetExtendedTcpTable(buffer, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL);
            if (ret == 0)
            {
                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = IntPtr.Add(buffer, 4);
                int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

                for (int i = 0; i < numEntries; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                    if (row.state == MIB_TCP_STATE_LISTEN)
                    {
                        int port = (row.localPort1 << 8) + row.localPort2;
                        if (row.owningPid > 4)
                        {
                            list.Add((port, row.owningPid));
                        }
                    }
                    rowPtr = IntPtr.Add(rowPtr, rowSize);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return list;
    }

    private static string GetCommandLine(int pid)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {pid}");
            foreach (var o in searcher.Get())
            {
                var obj = (ManagementObject)o;
                return obj["CommandLine"]?.ToString() ?? "";
            }
        }
        catch
        {
            // Ignore
        }

        return "";
    }

    private static (string Framework, string AppName) IdentifyFrameworkAndApp(string processName, string cmdLine)
    {
        string framework = "Node.js";
        string appName = "Local App";

        var lowerCmd = cmdLine.ToLowerInvariant();

        if (lowerCmd.Contains("next") || lowerCmd.Contains(".next"))
        {
            framework = "Next.js";
        }
        else if (lowerCmd.Contains("vite"))
        {
            framework = "Vite";
        }
        else if (lowerCmd.Contains("react-scripts"))
        {
            framework = "React";
        }
        else if (lowerCmd.Contains("nest"))
        {
            framework = "NestJS";
        }
        else if (lowerCmd.Contains("bun"))
        {
            framework = "Bun";
        }

        // Try to extract directory name from command line
        try
        {
            var parts = cmdLine.Split(['"', ' '], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains(":\\") && !part.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = Path.GetDirectoryName(part);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        var dirName = Path.GetFileName(dir);
                        if (!string.IsNullOrEmpty(dirName) && !dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) && !dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                        {
                            appName = dirName;
                            break;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        return (framework, appName);
    }
}
