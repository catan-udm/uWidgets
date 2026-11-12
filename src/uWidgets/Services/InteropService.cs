using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Win32;

namespace uWidgets.Services;

public enum WallpaperFitMode
{
    Fill,
    Fit,
    Stretch,
    Tile,
    Center,
    Span
}

public class InteropService
{
    private const int SPI_GETDESKWALLPAPER = 0x0073;
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    private const long WS_CHILD = 0x40000000L;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint WM_ACTIVATE = 0x0006;
    private const uint WM_ACTIVATEAPP = 0x001C;
    private const uint WM_WINDOWPOSCHANGING = 0x0046;
    private const uint WM_WINDOWPOSCHANGED = 0x0047;
    private const uint WM_SYSCOMMAND = 0x0112;
    private const int SC_MINIMIZE = 0xF020;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOOWNERZORDER = 0x0200;
    private const uint SWP_NOSENDCHANGING = 0x0400;
    private const uint SWP_ASYNCWINDOWPOS = 0x4000;

    private const uint GW_HWNDPREV = 3;

    private const int DWMWA_NCRENDERING_POLICY = 2;
    private const uint DWMNCRP_USEWINDOWSTYLE = 0;
    private const uint DWMNCRP_DISABLED = 1;
    private const int DWMWA_FLIP3D_POLICY = 8;
    private const uint DWMFLIP3D_EXCLUDEBELOW = 1;
    private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
    private const int DWMWA_CLOAKED = 14;
    private const int DWMWA_BORDER_COLOR = 34;
    private const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;
    private const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
    private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private static readonly IntPtr HWND_BOTTOM = new(1);

    private sealed class PinnedWidgetEntry(Window window, Win32Properties.CustomWndProcHookCallback hook)
    {
        public Window Window { get; } = window;
        public Win32Properties.CustomWndProcHookCallback Hook { get; } = hook;
    }

    private static readonly Dictionary<IntPtr, PinnedWidgetEntry> PinnedWidgets = new();
    private static readonly HashSet<IntPtr> InBandScratch = new();
    private static WinEventDelegate? winEventCallback;
    private static IntPtr foregroundHookHandle;
    private static IntPtr minimizeHookHandle;
    private static DispatcherTimer? desktopWatchdogTimer;
    private static bool isSyncingZOrder;
    private static bool systemEventsSubscribed;
    private static bool isSessionLocked;

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

    public static string GetWallpaperPath()
    {
        StringBuilder wallpaperPath = new StringBuilder(260);
        SystemParametersInfo(SPI_GETDESKWALLPAPER, wallpaperPath.Capacity, wallpaperPath, 0);
        return wallpaperPath.ToString();
    }

    public static WallpaperFitMode GetWallpaperFitMode()
    {
        if (!OperatingSystem.IsWindows())
            return WallpaperFitMode.Fill;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            var style = key?.GetValue("WallpaperStyle")?.ToString() ?? "10";
            var tile = key?.GetValue("TileWallpaper")?.ToString() ?? "0";

            if (tile == "1")
                return WallpaperFitMode.Tile;

            return style switch
            {
                "10" => WallpaperFitMode.Fill,
                "6" => WallpaperFitMode.Fit,
                "2" => WallpaperFitMode.Stretch,
                "22" => WallpaperFitMode.Span,
                "0" => WallpaperFitMode.Center,
                _ => WallpaperFitMode.Fill
            };
        }
        catch
        {
            return WallpaperFitMode.Fill;
        }
    }

    public static void RemoveWindowFromAltTab(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle == null || handle.Value == IntPtr.Zero)
            return;

        ApplyDesktopToolWindowAttributes(handle.Value);
    }

    public static void PinWindowToDesktop(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle == null || handle.Value == IntPtr.Zero)
            return;

        var hwnd = handle.Value;
        ApplyDesktopToolWindowAttributes(hwnd);

        if (!PinnedWidgets.ContainsKey(hwnd))
        {
            Win32Properties.CustomWndProcHookCallback hook = (IntPtr targetHwnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                WidgetWndProc(window, targetHwnd, msg, wParam, lParam, ref handled);

            Win32Properties.AddWndProcHookCallback(window, hook);
            PinnedWidgets[hwnd] = new PinnedWidgetEntry(window, hook);
            EnsureDesktopHooksRunning();
        }

        EnforceDesktopZOrder(window);
    }

    public static void UnpinWindowFromDesktop(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle != null && handle.Value != IntPtr.Zero && PinnedWidgets.Remove(handle.Value, out var entry))
        {
            Win32Properties.RemoveWndProcHookCallback(window, entry.Hook);
        }
        else
        {
            IntPtr? matchKey = null;
            foreach (var kvp in PinnedWidgets)
            {
                if (ReferenceEquals(kvp.Value.Window, window))
                {
                    matchKey = kvp.Key;
                    Win32Properties.RemoveWndProcHookCallback(window, kvp.Value.Hook);
                    break;
                }
            }

            if (matchKey.HasValue)
                PinnedWidgets.Remove(matchKey.Value);
        }

        if (PinnedWidgets.Count == 0)
            StopDesktopHooks();
    }

    public static void ApplyLayeringMode(Window window, uWidgets.Core.Models.Settings.LayeringMode mode)
    {
        switch (mode)
        {
            case uWidgets.Core.Models.Settings.LayeringMode.Normal:
                UnpinWindowFromDesktop(window);
                window.Topmost = false;
                RemoveWindowFromAltTab(window);
                break;
            case uWidgets.Core.Models.Settings.LayeringMode.AlwaysOnTop:
                UnpinWindowFromDesktop(window);
                window.Topmost = true;
                RemoveWindowFromAltTab(window);
                break;
            case uWidgets.Core.Models.Settings.LayeringMode.DesktopPinned:
            default:
                window.Topmost = false;
                PinWindowToDesktop(window);
                break;
        }
    }

    public static void EnforceDesktopZOrder(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle == null || handle.Value == IntPtr.Zero)
            return;

        if (window.OwnedWindows.Count > 0)
            return;

        var hwnd = handle.Value;
        var targetInsertAfter = GetDesktopBandTarget(hwnd, out var alreadyInBand);
        if (alreadyInBand)
            return;

        isSyncingZOrder = true;
        try
        {
            SetWindowPos(
                hwnd,
                targetInsertAfter,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
        }
        finally
        {
            isSyncingZOrder = false;
        }
    }

    private static void ApplyDesktopToolWindowAttributes(IntPtr hwnd)
    {
        var exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
        var desiredExStyle = (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
        if (desiredExStyle != exStyle)
            SetWindowLong(hwnd, GWL_EXSTYLE, desiredExStyle);

        try
        {
            uint excludeFromPeek = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excludeFromPeek, sizeof(uint));

            uint flip3dPolicy = DWMFLIP3D_EXCLUDEBELOW;
            DwmSetWindowAttribute(hwnd, DWMWA_FLIP3D_POLICY, ref flip3dPolicy, sizeof(uint));
        }
        catch
        {
            // Ignore on older Windows versions
        }
    }

    private static IntPtr WidgetWndProc(
        Window window,
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        try
        {
            if (msg == WM_SYSCOMMAND && ((int)wParam & 0xFFF0) == SC_MINIMIZE)
            {
                handled = true;
                return IntPtr.Zero;
            }

            if (msg == WM_WINDOWPOSCHANGING && lParam != IntPtr.Zero && !isSyncingZOrder && window.OwnedWindows.Count == 0)
            {
                var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                if ((pos.flags & SWP_NOZORDER) == 0)
                {
                    var targetInsertAfter = GetDesktopBandTarget(hWnd, out var alreadyInBand);
                    if (alreadyInBand)
                    {
                        pos.flags |= SWP_NOZORDER;
                    }
                    else
                    {
                        pos.hwndInsertAfter = targetInsertAfter;
                    }

                    Marshal.StructureToPtr(pos, lParam, false);
                }
            }
            else if ((msg == WM_ACTIVATE || msg == WM_ACTIVATEAPP || msg == WM_WINDOWPOSCHANGED) && !isSyncingZOrder && window.OwnedWindows.Count == 0)
            {
                Dispatcher.UIThread.Post(SyncPinnedWidgetsZOrder, DispatcherPriority.Send);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InteropService] WidgetWndProc error: {ex}");
        }

        return IntPtr.Zero;
    }

    private static void EnsureDesktopHooksRunning()
    {
        if (winEventCallback == null)
        {
            winEventCallback = OnWinEvent;
            foregroundHookHandle = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                winEventCallback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

            minimizeHookHandle = SetWinEventHook(
                EVENT_SYSTEM_MINIMIZESTART,
                EVENT_SYSTEM_MINIMIZEEND,
                IntPtr.Zero,
                winEventCallback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        }

        if (desktopWatchdogTimer == null)
        {
            desktopWatchdogTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            desktopWatchdogTimer.Tick += (_, _) =>
            {
                if (!isSessionLocked)
                    SyncPinnedWidgetsZOrder();
            };
            if (!isSessionLocked)
                desktopWatchdogTimer.Start();
        }

        if (!systemEventsSubscribed && OperatingSystem.IsWindows())
        {
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            systemEventsSubscribed = true;
        }
    }

    private static void StopDesktopHooks()
    {
        if (foregroundHookHandle != IntPtr.Zero)
        {
            UnhookWinEvent(foregroundHookHandle);
            foregroundHookHandle = IntPtr.Zero;
        }

        if (minimizeHookHandle != IntPtr.Zero)
        {
            UnhookWinEvent(minimizeHookHandle);
            minimizeHookHandle = IntPtr.Zero;
        }

        winEventCallback = null;

        if (desktopWatchdogTimer != null)
        {
            desktopWatchdogTimer.Stop();
            desktopWatchdogTimer = null;
        }

        if (systemEventsSubscribed && OperatingSystem.IsWindows())
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            systemEventsSubscribed = false;
        }
    }

    private static void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (!OperatingSystem.IsWindows()) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                isSessionLocked = true;
                desktopWatchdogTimer?.Stop();
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                isSessionLocked = false;
                desktopWatchdogTimer?.Start();
                SyncPinnedWidgetsZOrder();
            }
        });
    }

    private static void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var entry in new List<PinnedWidgetEntry>(PinnedWidgets.Values))
            {
                if (entry.Window is Views.Widget widget)
                    widget.RecoverIfOffscreen();
            }
        }, DispatcherPriority.Background);
    }

    private static void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (PinnedWidgets.Count == 0)
            return;

        Dispatcher.UIThread.Post(SyncPinnedWidgetsZOrder, DispatcherPriority.Background);
    }

    private static void SyncPinnedWidgetsZOrder()
    {
        if (isSyncingZOrder || PinnedWidgets.Count == 0)
            return;

        try
        {
            var desktopHost = GetDesktopHostWindow();
            if (desktopHost == IntPtr.Zero)
                return;

            InBandScratch.Clear();
            var backmostNormalWindow = IntPtr.Zero;

            var curr = GetWindow(desktopHost, GW_HWNDPREV);
            while (curr != IntPtr.Zero)
            {
                if (PinnedWidgets.ContainsKey(curr))
                {
                    InBandScratch.Add(curr);
                }
                else if (IsNormalTopLevelWindow(curr))
                {
                    backmostNormalWindow = curr;
                    break;
                }

                curr = GetWindow(curr, GW_HWNDPREV);
            }

            var targetInsertAfter = backmostNormalWindow != IntPtr.Zero ? backmostNormalWindow : HWND_TOP;

            isSyncingZOrder = true;
            try
            {
                foreach (var (hwnd, entry) in PinnedWidgets)
                {
                    if (InBandScratch.Contains(hwnd) || entry.Window.OwnedWindows.Count > 0)
                        continue;

                    SetWindowPos(
                        hwnd,
                        targetInsertAfter,
                        0,
                        0,
                        0,
                        0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING | SWP_ASYNCWINDOWPOS);
                }
            }
            finally
            {
                isSyncingZOrder = false;
            }
        }
        catch (Exception ex)
        {
            isSyncingZOrder = false;
            System.Diagnostics.Debug.WriteLine($"[InteropService] SyncPinnedWidgetsZOrder error: {ex}");
        }
    }

    private static IntPtr GetDesktopBandTarget(IntPtr widgetHwnd, out bool alreadyInBand)
    {
        alreadyInBand = false;
        var desktopHost = GetDesktopHostWindow();
        if (desktopHost == IntPtr.Zero)
            return HWND_BOTTOM;

        var curr = GetWindow(desktopHost, GW_HWNDPREV);
        while (curr != IntPtr.Zero)
        {
            if (curr == widgetHwnd)
            {
                alreadyInBand = true;
            }
            else if (!PinnedWidgets.ContainsKey(curr) && IsNormalTopLevelWindow(curr))
            {
                return curr;
            }

            curr = GetWindow(curr, GW_HWNDPREV);
        }

        return HWND_TOP;
    }

    private static IntPtr GetDesktopHostWindow()
    {
        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero && FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
            return progman;

        var workerW = IntPtr.Zero;
        while ((workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null)) != IntPtr.Zero)
        {
            if (FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                return workerW;
        }

        return progman != IntPtr.Zero ? progman : GetShellWindow();
    }

    private static bool IsNormalTopLevelWindow(IntPtr hwnd)
    {
        if (!IsWindowVisible(hwnd) || IsIconic(hwnd))
            return false;

        var style = GetWindowLong(hwnd, GWL_STYLE).ToInt64();
        if ((style & WS_CHILD) != 0)
            return false;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE).ToInt64();
        if ((exStyle & (WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE)) != 0)
            return false;

        if (DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
            return false;

        if (!GetWindowRect(hwnd, out var rect) || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            return false;

        var classNameBuilder = new StringBuilder(64);
        if (GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity) > 0)
        {
            var className = classNameBuilder.ToString();
            if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Windows.UI.Core.CoreWindow")
                return false;
        }

        return true;
    }

    public static void SetLiquidGlassWindowAttributes(Window window, bool enabled)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle == null || handle.Value == IntPtr.Zero)
            return;

        try
        {
            uint borderColor = enabled ? DWMWA_COLOR_NONE : DWMWA_COLOR_DEFAULT;
            DwmSetWindowAttribute(handle.Value, DWMWA_BORDER_COLOR, ref borderColor, sizeof(uint));

            uint ncPolicy = enabled ? DWMNCRP_DISABLED : DWMNCRP_USEWINDOWSTYLE;
            DwmSetWindowAttribute(handle.Value, DWMWA_NCRENDERING_POLICY, ref ncPolicy, sizeof(uint));
        }
        catch
        {
            // Ignore on older Windows versions that do not support DWMWA_BORDER_COLOR
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    private static void SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        int error;
        IntPtr result;

        SetLastError(0);

        if (IntPtr.Size == 4)
        {
            var tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
            error = Marshal.GetLastWin32Error();
            result = new IntPtr(tempResult);
        }
        else
        {
            result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
            error = Marshal.GetLastWin32Error();
        }

        if (result == IntPtr.Zero && error != 0) throw new Win32Exception(error);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int IntSetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private static int IntPtrToInt32(IntPtr intPtr)
    {
        return unchecked((int)intPtr.ToInt64());
    }

    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    private static extern void SetLastError(int dwErrorCode);
}