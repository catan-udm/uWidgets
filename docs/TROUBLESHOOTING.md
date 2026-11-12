# uWidgets Troubleshooting Guide

## Widgets Are Off-Screen After Changing Monitors or Resolution

When monitors are disconnected, rearranged, or DPI scaling changes, widgets saved at old coordinates may end up outside visible screen bounds.

- **Automatic Recovery**: uWidgets listens for display setting changes and automatically brings widgets back inside a valid monitor working area.
- **Manual Recovery**:
  1. Open **Settings -> Widget Manager -> Active Widgets** (press `Ctrl+Shift+U` if no widget is visible).
  2. Click **Recover Offscreen** to move all off-screen widgets onto the active screen, or click **Center** / **Highlight** next to a specific widget.

---

## Closing Settings Doesn't Exit uWidgets

uWidgets runs as a background desktop host with **explicit shutdown** so closing the Settings window leaves your desktop widgets running.

- Closing Settings (`[X]`) hides the Settings window rather than quitting the app.
- To fully quit uWidgets and close all widgets, right-click the **System Tray icon** and choose **Quit** / **Exit**.
- If you hid the tray icon in **Settings -> General**, press `Ctrl+Shift+U` to reopen Settings at any time.

---

## Global Hotkey Conflict Warning

If **Settings -> General** shows a hotkey registration warning banner:

1. Another running program (such as a screen capture tool, window manager, or game overlay) has already registered that key combination with Windows.
2. Close or reconfigure the conflicting application, or disable unused hotkeys in **Settings -> General**.

---

## Settings or Layouts Don't Save (Read-Only Install Folder)

If uWidgets is installed inside a protected folder such as `C:\Program Files\uWidgets`:

- Standard mode automatically stores mutable files (`appSettings.json`, `layout.json`, `Profiles\`, `Logs\`) inside `%APPDATA%\uWidgets\`.
- If you intentionally enabled **Portable Mode** (`portable.dat` or `UWIDGETS_PORTABLE=1`) inside a read-only folder where write checks fail, uWidgets falls back to a writable directory (`%APPDATA%\uWidgets\` or temp). Move your portable folder to a user-writable path such as `C:\Tools\uWidgets\` or `%LOCALAPPDATA%\uWidgets\`.

---

## Weather Widget Shows "Offline" or Rate Limit Countdown

- The Open-Meteo API enforces request rate limits (`HTTP 429`) and requires internet access.
- When offline or rate-limited, the Forecast widget preserves your last fetched forecast alongside a **`Last updated HH:mm (Cached)`** banner and a **Retry** button.
- After an HTTP 429 response, a **30-second cooldown** runs before **Retry** becomes active again.
- Network error details are written to `weather_crash_log.txt` inside the **Logs** folder (**Settings -> About -> Open Logs Folder**).

---

## Corrupt Configuration or Crash Recovery

uWidgets uses atomic file replacement (`.tmp` + `.bak` backup fallback) when saving `appSettings.json` and `layout.json`.

- If a configuration issue still prevents normal behavior:
  - Open **Settings -> About -> Open Logs Folder** to inspect `crash_log.txt` / `weather_crash_log.txt`.
  - Click **Settings -> About -> Reset Settings to Defaults** to restore default application preferences without losing your widget DLLs.
  - To inspect files manually, click **Settings -> About -> Open Data Folder** (typically `%APPDATA%\uWidgets\`).

---

## Widgets Disappear Behind the Desktop or After Locking Windows

- Check **Settings -> General -> Layering Mode**:
  - **DesktopPinned**: Keeps widgets anchored at desktop level (above wallpaper, below normal windows).
  - **Normal**: Standard desktop windows behavior.
  - **AlwaysOnTop**: Keeps widgets above other windows.
- If a third-party desktop/wallpaper tool interferes with z-order, switch Layering Mode to **Normal** or **AlwaysOnTop**, or press `Win+Shift+W` twice to toggle visibility.
