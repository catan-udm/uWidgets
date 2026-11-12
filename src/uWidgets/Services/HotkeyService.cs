using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Settings;

namespace uWidgets.Services;

public sealed class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const int GWLP_WNDPROC = -4;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private const int IdToggleWidgets = 1;
    private const int IdToggleLock = 2;
    private const int IdOpenSettings = 3;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly Action onToggleWidgets;
    private readonly Action onToggleLock;
    private readonly Action onOpenSettings;
    private readonly List<string> registrationWarnings = [];

    private IntPtr messageHwnd;
    private IntPtr previousWndProc;
    private WndProcDelegate? wndProcDelegate;
    private bool disposed;

    public IReadOnlyList<string> RegistrationWarnings => registrationWarnings;
    public event EventHandler? WarningsChanged;

    public HotkeyService(
        IAppSettingsProvider appSettingsProvider,
        Action onToggleWidgets,
        Action onToggleLock,
        Action onOpenSettings)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.onToggleWidgets = onToggleWidgets;
        this.onToggleLock = onToggleLock;
        this.onOpenSettings = onOpenSettings;

        if (OperatingSystem.IsWindows())
        {
            InitializeMessageWindow();
            ApplySettings(appSettingsProvider.Get().EffectiveHotkeys);
            appSettingsProvider.DataChanged += OnSettingsChanged;
        }
    }

    private void OnSettingsChanged(object sender, AppSettings? oldData, AppSettings newData)
    {
        if (oldData?.EffectiveHotkeys != newData.EffectiveHotkeys)
        {
            Dispatcher.UIThread.Post(() => ApplySettings(newData.EffectiveHotkeys));
        }
    }

    private void InitializeMessageWindow()
    {
        try
        {
            messageHwnd = CreateWindowEx(0, "STATIC", "uWidgetsHotkeySink", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (messageHwnd == IntPtr.Zero) return;

            wndProcDelegate = MessageWindowProc;
            var procPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
            previousWndProc = SetWindowLongPtr(messageHwnd, GWLP_WNDPROC, procPtr);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HotkeyService] Message window init failed: {ex.Message}");
        }
    }

    public void ApplySettings(HotkeySettings hotkeys)
    {
        if (!OperatingSystem.IsWindows() || messageHwnd == IntPtr.Zero) return;

        UnregisterAll();
        registrationWarnings.Clear();

        if (!hotkeys.Enabled)
        {
            WarningsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        TryRegister(IdToggleWidgets, hotkeys.ToggleWidgets, "Show / Hide Widgets");
        TryRegister(IdToggleLock, hotkeys.ToggleLock, "Lock / Unlock Positions");
        TryRegister(IdOpenSettings, hotkeys.OpenSettings, "Open Settings");

        WarningsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TryRegister(int id, string shortcut, string label)
    {
        if (!TryParseShortcut(shortcut, out var modifiers, out var vk))
        {
            registrationWarnings.Add($"{label}: invalid shortcut '{shortcut}'.");
            return;
        }

        if (!RegisterHotKey(messageHwnd, id, modifiers | MOD_NOREPEAT, vk))
        {
            var err = Marshal.GetLastWin32Error();
            registrationWarnings.Add($"{label} ({shortcut}): unavailable or already used by another app (Win32 error {err}).");
        }
    }

    private void UnregisterAll()
    {
        if (messageHwnd == IntPtr.Zero) return;
        UnregisterHotKey(messageHwnd, IdToggleWidgets);
        UnregisterHotKey(messageHwnd, IdToggleLock);
        UnregisterHotKey(messageHwnd, IdOpenSettings);
    }

    private IntPtr MessageWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            switch (wParam.ToInt32())
            {
                case IdToggleWidgets:
                    Dispatcher.UIThread.Post(onToggleWidgets);
                    return IntPtr.Zero;
                case IdToggleLock:
                    Dispatcher.UIThread.Post(onToggleLock);
                    return IntPtr.Zero;
                case IdOpenSettings:
                    Dispatcher.UIThread.Post(onOpenSettings);
                    return IntPtr.Zero;
            }
        }

        return CallWindowProc(previousWndProc, hWnd, msg, wParam, lParam);
    }

    public static bool TryParseShortcut(string? shortcut, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;
        if (string.IsNullOrWhiteSpace(shortcut)) return false;

        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToUpperInvariant())
            {
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                case "WIN":
                case "WINDOWS":
                case "SUPER":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    return false;
            }
        }

        var keyToken = parts[^1].ToUpperInvariant();
        if (keyToken.Length == 1 && keyToken[0] is >= 'A' and <= 'Z')
        {
            virtualKey = keyToken[0];
            return modifiers != 0;
        }

        if (keyToken.Length == 1 && keyToken[0] is >= '0' and <= '9')
        {
            virtualKey = keyToken[0];
            return modifiers != 0;
        }

        if (keyToken.StartsWith('F') && int.TryParse(keyToken[1..], out var fn) && fn is >= 1 and <= 12)
        {
            virtualKey = (uint)(0x70 + (fn - 1));
            return modifiers != 0;
        }

        return false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (OperatingSystem.IsWindows())
        {
            appSettingsProvider.DataChanged -= OnSettingsChanged;
            UnregisterAll();
            if (messageHwnd != IntPtr.Zero)
            {
                DestroyWindow(messageHwnd);
                messageHwnd = IntPtr.Zero;
            }
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
