using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
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
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int GWL_EXSTYLE = -20;

        var handle = window.TryGetPlatformHandle()?.Handle;
        
        if (handle == null) return;

        var exStyle = (int)GetWindowLong(handle.Value, GWL_EXSTYLE);

        exStyle |= WS_EX_TOOLWINDOW;
        SetWindowLong(handle.Value, GWL_EXSTYLE, exStyle);
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
            const int DWMWA_NCRENDERING_POLICY = 2;
            const uint DWMNCRP_USEWINDOWSTYLE = 0;
            const uint DWMNCRP_DISABLED = 1;
            const int DWMWA_BORDER_COLOR = 34;
            const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;
            const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

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