# uWidgets User Guide

Welcome to **uWidgets** — a modular desktop widget suite for Windows built with .NET 8 and Avalonia UI.

---

## 1. First Launch & Welcome Tour

When you start uWidgets for the first time, the **Welcome Wizard** guides you through setup in three quick steps:

1. **Welcome**: Overview of desktop pinning, grid snapping, and tray management.
2. **Choose a Starter Pack**:
   - **Essential**: Balanced starter workspace with Clock, Calendar, Weather, and Notes.
   - **Developer**: Workspace focused on development workflows (Git Working Tree, System Dashboard, Digital Clock, and Notes).
   - **Minimalist**: Clean setup with Analog Clock and Weather Forecast.
   - **Clean Slate**: Start with an empty desktop and add widgets manually later.
3. **Desktop Gestures Tour**: Quick visual reference showing how to move, resize, edit, and lock widgets.

You can replay the tour at any time from **Settings -> Help & Guide -> Replay Welcome Tour**.

---

## 2. Managing Widgets

### Adding & Removing Widgets
- **Settings -> Widget Manager** (or **Add Widget** in the gallery): Browse all available widgets by category or search by name, inspect active desktop instances, highlight/locate a widget on screen, reset an off-screen widget to screen center, or remove individual widgets.
- **Gallery Pages**: Select a category in the Settings sidebar, pick a size preset (**Small 2x2**, **Medium 4x2**, **Large 4x4**, **Extra Large 8x4**), and add it directly. When added from Settings, smart placement places new widgets clear of existing widgets and clear of the Settings window, accompanied by a confirmation banner.

### Editing a Widget
- **Right-click** any widget and choose **Edit Widget** to customize widget-specific options (city, timezone, repository path, photo folder, metrics, etc.) or change its size preset.
- Widgets with inline text fields (**Notes**, **Reminders**) can be typed into directly on the desktop — click or double-click the text field to focus and type.

### Positioning, Resizing & Locking
- **Move**: Drag anywhere on the widget surface to reposition it. With **Virtual Grid** enabled, widgets snap to grid cells on release.
- **Resize**: Drag the bottom-right corner handle.
- **Lock**: Toggle **Master Lock** in **Settings -> Desktop Grid** (or press `Win+Alt+L`), or individually lock position/size in **Desktop Grid**.

---

## 3. Settings Overview

Open Settings anytime from the **System Tray icon**, the **right-click widget menu**, or by pressing `Ctrl+Shift+U`. Press `Ctrl+F` inside Settings to filter controls by keyword.

| Section | What You Configure |
| --- | --- |
| **General** | Start with Windows, Show System Tray icon, Window Layering Mode (`DesktopPinned`, `Normal`, `AlwaysOnTop`), Global Hotkeys (`Win+Shift+W`, `Win+Alt+L`, `Ctrl+Shift+U`), Automatic Update Checks, and Display Language (with inline Restart banner). |
| **Widget Manager** | **Active Widgets** list (Highlight, Center, Show/Hide, Edit, Remove, Recover Offscreen), searchable **Catalog**, and **Layout Profiles** (Save, Load, Delete, Export/Import JSON). |
| **Appearance** | Live Widget Preview Card, Dark/Light theme, 8 theme templates, Accent color, Monochrome mode, Widget Opacity (0-100%), Corner Radius, Font family, and Native Frame option. |
| **Visual Effects** | Liquid Glass surface toggle, 3 curated presets (*Subtle Frost*, *Frosted Meniscus*, *High Refraction Crystal*), and collapsible *Optics Fine-Tuning (Advanced)* controls. |
| **Desktop Grid** | Master Lock, Virtual Grid snapping toggles (`Lock Position`, `Lock Size`), grid cell size presets (`48`, `72`, `96`), widget spacing/margin, and alignment. |
| **Help & Guide** | Replay Welcome Tour, desktop gesture reference, active global shortcuts, and quick links to open Data & Logs folders. |
| **About** | App version, Open Data Folder, Open Logs Folder, and Reset Settings to Defaults. |

---

## 4. Layout Profiles

In **Settings -> Widget Manager -> Profiles**, you can:
- **Save** your current widget arrangement as a named JSON profile in the `Profiles` folder.
- **Switch / Load** between saved profiles (e.g., *Work*, *Minimal*, *Multi-Monitor*).
- **Export / Import** `.json` profile files to share or back up setups across machines.

---

## 5. Data Storage & Portable Mode

By default, uWidgets stores user configuration and runtime data in a writable user data directory:

- **Installed / Standard Mode**: `%APPDATA%\uWidgets\`
  - `appSettings.json` — Global preferences, theme, hotkeys, and onboarding state
  - `layout.json` — Active widget positions, sizes, and per-widget settings
  - `Profiles\` — Saved layout profiles
  - `Logs\` — Application crash logs and weather provider logs
  - `CustomIcons\` / cache files — Launcher icons and widget caches
- **Legacy Migration**: On first launch after upgrading from an older portable build, existing config/cache/icon files next to `uWidgets.exe` are automatically copied to `%APPDATA%\uWidgets\` while keeping the install folder safe when installed under read-only paths like `Program Files`.
- **Portable Mode**: Create an empty `portable.dat` file next to `uWidgets.exe` (or set the environment variable `UWIDGETS_PORTABLE=1`). When the executable directory is writable, uWidgets stores all data alongside the executable instead of `%APPDATA%\uWidgets`.
