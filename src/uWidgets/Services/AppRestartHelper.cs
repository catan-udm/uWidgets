using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace uWidgets.Services;

/// <summary>
/// Centralizes restarting and shutting down the desktop application safely.
/// </summary>
public static class AppRestartHelper
{
    public static void RestartApplication(string arguments = "--settings --restarted")
    {
        SingleInstanceService.StopPipeServer();
        SingleInstanceService.ReleaseMutex();

        var executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(executablePath))
        {
            using var process = Process.Start(executablePath, arguments);
            var tryCount = 0;
            while (process != null && process.MainWindowHandle == IntPtr.Zero && !process.HasExited && tryCount++ < 10)
            {
                Thread.Sleep(100);
                process.Refresh();
            }
        }

        ShutdownApplication();
    }

    public static void ShutdownApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
