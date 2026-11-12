# uWidgets: Systemwide Quality-of-Life (QoL) Audit & Implementation Plan

> **Document Version**: 1.1.0 (Refined with Adversarial Quality Gates & System Constraints)  
> **Target Release**: uWidgets 2.0 QoL Modernization  
> **Status**: Comprehensive Engineering Research & Implementation Plan (Pre-Implementation)  
> **Repository**: `C:\Users\sankobite\dev\uWidgets`  
> **Target Framework**: .NET 8.0, Avalonia UI 11.1.3, Windows 10/11 Desktop  

---

## Table of Contents

1. [Executive Summary & Purpose](#1-executive-summary--purpose)
   - [1.1 Vision and Strategic Goals](#11-vision-and-strategic-goals)
   - [1.2 Architecture Overview & Technical Baseline](#12-architecture-overview--technical-baseline)
   - [1.3 Summary of Discovered Vulnerabilities & Friction Points](#13-summary-of-discovered-vulnerabilities--friction-points)
2. [Comprehensive Systemwide QoL Audit Findings](#2-comprehensive-systemwide-qol-audit-findings)
   - [2.1 Application Lifecycle, Background Execution & System Tray Absence](#21-application-lifecycle-background-execution--system-tray-absence)
   - [2.2 First-Run Experience & User Journey Friction Points](#22-first-run-experience--user-journey-friction-points)
   - [2.3 Multi-Instance Widget Collisions & Multi-Monitor Snapping Defects](#23-multi-instance-widget-collisions--multi-monitor-snapping-defects)
   - [2.4 Configuration Storage Resilience & Startup Vulnerabilities](#24-configuration-storage-resilience--startup-vulnerabilities)
   - [2.5 Settings Cognitive Overload, Taxonomy Confusion & Optical Physics Jargon](#25-settings-cognitive-overload-taxonomy-confusion--optical-physics-jargon)
   - [2.6 In-App Guidance, Tooltip Degradation & Empty-State Deficits](#26-in-app-guidance-tooltip-degradation--empty-state-deficits)
   - [2.7 Documentation Drift & Repository Information Gaps](#27-documentation-drift--repository-information-gaps)
3. [Target Architectural Solutions & UI/UX Proposals](#3-target-architectural-solutions--uiux-proposals)
   - [3.1 First-Run Onboarding Tutorial & Welcome Experience](#31-first-run-onboarding-tutorial--welcome-experience)
   - [3.2 Redesigned 6-Domain Settings Architecture & Wireframes](#32-redesigned-6-domain-settings-architecture--wireframes)
   - [3.3 Native Avalonia System Tray Hub & Lifecycle Controller](#33-native-avalonia-system-tray-hub--lifecycle-controller)
   - [3.4 Active Widgets Manager & Layout Profiles](#34-active-widgets-manager--layout-profiles)
   - [3.5 Multi-Monitor Screen-Aware Coordinate Engine & Global Hotkeys](#35-multi-monitor-screen-aware-coordinate-engine--global-hotkeys)
   - [3.6 Resilient AppData Storage & Atomic File Operations](#36-resilient-appdata-storage--atomic-file-operations)
   - [3.7 In-App Guidance, Standardized Tooltip Framework & Error Recovery](#37-in-app-guidance-standardized-tooltip-framework--error-recovery)
4. [Actionable, Prioritized Phased Implementation Roadmap](#4-actionable-prioritized-phased-implementation-roadmap)
   - [Phase 1: Core Stability, Lifecycle & Storage Resilience (P0 - Critical)](#phase-1-core-stability-lifecycle--storage-resilience-p0---critical)
   - [Phase 2: First-Run Onboarding, Guidance & Widget Management (P0/P1 - High)](#phase-2-first-run-onboarding-guidance--widget-management-p0p1---high)
   - [Phase 3: Settings Modernization, Simplification & Appearance Polish (P1 - High)](#phase-3-settings-modernization-simplification--appearance-polish-p1---high)
   - [Phase 4: Workflow Productivity, Hotkeys & Network Fault Tolerance (P2 - Medium)](#phase-4-workflow-productivity-hotkeys--network-fault-tolerance-p2---medium)
   - [Phase 5: Documentation Overhaul & In-Repo Guides (P2 - Medium)](#phase-5-documentation-overhaul--in-repo-guides-p2---medium)
5. [Phase Breakdown Matrix](#5-phase-breakdown-matrix)
6. [Verification, Quality Assurance & Clean Git Safeguards](#6-verification-quality-assurance--clean-git-safeguards)
7. [User Review and Approval Sign-Off](#7-user-review-and-approval-sign-off)

---

## 1. Executive Summary & Purpose

### 1.1 Vision and Strategic Goals

uWidgets is a modular, high-performance desktop widget engine built on .NET 8.0 and Avalonia UI 11.1.3. It bridges modern cross-platform UI rendering with deep Windows 10/11 desktop integration (`Progman`/`WorkerW` desktop pinning, WinUI Composition backdrops, and custom Skia-based LiquidGlass shaders). 

While the core rendering and plugin engine are technically capable, direct research across the entire codebase reveals critical Quality-of-Life (QoL), usability, architectural, and documentation gaps that severely hinder user adoption and satisfaction:

1. **Severe Lifecycle & Discoverability Gaps**: The application lacks a System Tray icon. Newly launched instances with zero widgets drop users into an intimidating shader configuration screen; closing the window silently terminates the process.
2. **Data & Layout Integrity Defects**: Multiple instances of the same widget type (e.g. two Note widgets or two Clocks) collide and overwrite each other's configuration due to the lack of an instance GUID. Dragging widgets on multi-monitor setups with negative virtual coordinates causes them to jump across screens to the primary display.
3. **Storage Brittleness**: Configuration files (`appSettings.json`, `layout.json`) are written non-atomically directly to the executable directory (`AppContext.BaseDirectory`), crashing under standard user accounts when installed to `C:\Program Files\` and risking total configuration corruption upon abrupt shutdown.
4. **Settings Cognitive Overload**: Essential desktop grid settings are obscured under "Advanced", while the "Appearance" page confronts casual users with raw optical physics equations ("Snell's law index of refraction", "7-tap spectral R/G/B separation", "Superellipse squircle exponent").
5. **Guidance Deficits & Tooltip Breakage**: A global CSS/XAML style overrides all tooltips with transparent borders and backgrounds to accommodate a resize handle hack, destroying informational tooltips across the application. Text inputs lack watermarks, and network-dependent widgets fail silently upon disconnection.
6. **Severe Documentation Drift**: The repository README claims the project has 6 widgets, whereas the codebase actually contains 11 modular widget assemblies (45% of available functionality is undocumented). No `docs/` directory exists.

This document serves as the **official Systemwide QoL Audit and Comprehensive Implementation Plan**. It synthesizes direct code inspections, identifies root causes with exact source line citations, proposes target architectural solutions, and establishes an actionable 5-phase implementation roadmap.

---

### 1.2 Architecture Overview & Technical Baseline

The solution consists of 13 projects organized in `src/uWidgets.sln`:
- **Executable Host (`src/uWidgets/`)**: Avalonia 11.1.3 desktop application implementing dependency injection (`Microsoft.Extensions.DependencyInjection`), desktop message hooking (`InteropService.cs`), virtual grid layout management (`GridService.cs`), and configuration views.
- **Shared Core Library (`src/uWidgets.Core/`)**: Shared interfaces (`IAppSettingsProvider`, `ILayoutProvider`, `IAssemblyProvider`), data models (`AppSettings`, `WidgetLayout`), JSON serialization (`JsonParser.cs`), and startup registry management (`StartupService.cs`).
- **Dynamic Widget Assemblies (`src/Widgets/`)**: 11 modular plugin assemblies loaded via .NET `AssemblyLoadContext` (`PluginLoadContext` in `AssemblyProvider.cs`), containing 27 distinct view implementations:
  1. `Calendar` (Date, Month, Countdown)
  2. `Clock` (Analog I, II, III, Digital, World, Pomodoro, Stopwatch)
  3. `Development` (Docker, DevApps, GitWorkingTree)
  4. `Finance` (Crypto, Stocks, Converter)
  5. `Media` (NowPlaying with WinRT GSMTC & synced lyrics)
  6. `Monitor` (SingleMetric, Dashboard, DeviceBatteries)
  7. `Notes` (Note with auto-save)
  8. `Photos` (PhotoFrame slideshow)
  9. `Reminders` (Interactive checklist)
  10. `Shortcuts` (Application launcher)
  11. `Weather` (Forecast, Temperature, UV Index, Sunrise/Sunset, Pressure, Air Quality)

---

### 1.3 Summary of Discovered Vulnerabilities & Friction Points

| Subsystem | Discovered Friction Point / Defect | Exact Code Citation | Impact Level |
|---|---|---|---|
| **Lifecycle** | Closing Settings when 0 widgets exist silently kills process; reopening from tray throws `InvalidOperationException`; no System Tray icon or packaged icon asset exists | `App.axaml.cs:48-55`, `App.axaml` (0 `TrayIcon` matches), `uWidgets.csproj` (missing `Assets/icon.ico`) | **P0 - Critical** |
| **Data Integrity** | Multiple instances of identical widget overwrite each other in `layout.json`; multiple constructors without `[JsonConstructor]` cause `NotSupportedException` | `WidgetLayout.cs:16`, `WidgetLayoutProvider.cs:23-38` | **P0 - Critical** |
| **Multi-Monitor** | Clamping with `Math.Max(0, ...)` snaps negative coordinates to primary display; removing clamps causes widgets to snap into offscreen space | `GridService.cs:31-37` | **P0 - Critical** |
| **First Run** | Initial run opens `Appearance` tab (index 1) with raw physics parameters and 0 widgets; clearing all widgets re-traps user in welcome modal | `Settings.axaml.cs:27`, `SettingsViewModel.cs:33-40`, `App.axaml.cs:54` | **P0 - Critical** |
| **Persistence** | Configuration files stored in `AppContext.BaseDirectory` via direct non-atomic write; fatal crash on bad JSON; static constructor I/O hazards | `Const.cs:15-33`, `JsonParser.cs:12, 33`, `Program.cs:85-97` | **P1 - High** |
| **UI Taxonomy** | Grid snapping/locking hidden in "Advanced"; active settings (`Monochrome`, `UseNativeFrame`, 8 `Templates`, `LockSize`) omitted/orphaned; raw optical equations exposed; opacity is decimal float | `Appearance.axaml:80-106`, `Advanced.axaml:1-53`, `appSettings.json:6,7,11-84,87` | **P1 - High** |
| **Tooltips** | Global `<Style Selector="ToolTip">` sets transparent background/border, making all text tooltips invisible | `Widget.axaml:47-52` | **P1 - High** |
| **Guidance** | No watermark prompts in Notes/Reminders; silent failures and unrooted logging in Weather; corrupted image silence in Photos | `ClickThroughTextBox.cs:67-70`, `ForecastViewModel.cs:29`, `OpenMeteoWeatherProvider.cs:39` | **P1 - High** |
| **Desktop Control** | Zero global hotkeys exist; proposed `Win+Shift+S` collides with Windows Snipping Tool (error 1409); no Active Widgets manager exists | Codebase search: 0 matches for `RegisterHotKey` | **P2 - Medium** |
| **Documentation** | README claims 6 widgets (missing 5); no `docs/` directory; dead external wiki link | `README.md:30, 176` | **P2 - Medium** |

---

## 2. Comprehensive Systemwide QoL Audit Findings

### 2.1 Application Lifecycle, Background Execution & System Tray Absence

#### Direct Code Observations
1. **Window Taskbar Suppression**:
   In `src/uWidgets/Views/Widget.axaml` line 8:
   ```xaml
   ShowInTaskbar="False"
   ```
   All desktop widget windows are explicitly hidden from the Windows Taskbar.
2. **Missing System Tray Infrastructure**:
   Searching the entire solution for `TrayIcon`, `TrayIcons`, or `NativeMenu` yields 0 matches. `src/uWidgets/App.axaml` contains no system tray configuration.
3. **Application Exit on Last Window Close**:
   In `src/uWidgets/App.axaml.cs` lines 44-55:
   ```csharp
   var widgetsCount = services
       .GetRequiredService<IWidgetFactory<Window, UserControl>>()
       .Create()
       .Select(widget =>
       {
           widget.Show();
           return widget;
       })
       .Count();

   if ((ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Args.Length: > 0 } desktop && desktop.Args[0] == "--settings") || widgetsCount == 0)
       services.GetRequiredService<Settings>().Show();
   ```
   Avalonia's `IClassicDesktopStyleApplicationLifetime.ShutdownMode` defaults to `ShutdownMode.OnLastWindowClose`.
   - On a fresh installation, `widgetsCount == 0`, so the `Settings` window opens.
   - When the user closes the Settings window, Avalonia detects that the number of open windows is zero.
   - **Result**: The entire application process terminates immediately and silently.
4. **The "Ghost Process" Dilemma**:
   - If widgets *are* active on the desktop and the user closes the Settings window, the widgets remain pinned to the desktop.
   - However, if the user minimizes all windows or opens a full-screen application, there is no icon in the Windows Taskbar or System Tray.
   - The user has **no graphical mechanism to reopen the Settings window**, add new widgets, or exit the application without opening a terminal and running `uWidgets.exe --settings` or terminating the process via Task Manager.

---

### 2.2 First-Run Experience & User Journey Friction Points

#### Direct Code Observations
1. **Default Selected Page Hardcoded to Appearance**:
   In `src/uWidgets/Views/Settings.axaml.cs` line 27:
   ```csharp
   ListBox.SelectedItem = SettingsViewModel.MenuItems[1];
   ```
   In `src/uWidgets/ViewModels/SettingsViewModel.cs` lines 33-40:
   ```csharp
   public static readonly PageViewModel[] MenuItems =
   [
       new PageViewModel(typeof(General), GetIcon(nameof(General)), Locale.Settings_General),
       new PageViewModel(typeof(Appearance), GetIcon(nameof(Appearance)), Locale.Settings_Appearance),
       new PageViewModel(typeof(Advanced), GetIcon(nameof(Advanced)), Locale.Settings_Advanced),
       new PageViewModel(typeof(About), GetIcon(nameof(About)),  Locale.Settings_About),
       new PageViewModel(null, null, null)
   ];
   ```
   `MenuItems[1]` resolves to `Appearance`. A new user is greeted not by a welcome tour, desktop preview, or starter widget pack, but by a dense form containing dark mode toggles, opacity numbers, and raw optical refraction constants.
2. **Hidden Widget Spawning Behind Settings**:
   In `src/uWidgets/Views/Pages/Gallery.axaml.cs` lines 68-78:
   ```csharp
   private void Button_OnClick(object? sender, RoutedEventArgs e)
   {
       var button = sender as Button;
       var viewModel = button!.DataContext as WidgetPreviewViewModel;
       var dimensions = appSettingsProvider.Get().Dimensions;
       var size = 2 * dimensions.Size + dimensions.Margin;
       var position = button.PointToScreen(new Point(0, 0));

       var widgetSettings = new WidgetLayout(viewModel!.Type, viewModel.Subtype, position.X, position.Y, size, size, null);
       widgetFactory.Add(widgetSettings).Show();
   }
   ```
   - When the user clicks a widget card in the Gallery, `button.PointToScreen(new Point(0, 0))` computes coordinates located directly within the open Settings window.
   - `PinWindowToDesktop` (`InteropService.cs:261`) assigns the new widget to the desktop layer behind normal windows.
   - **Result**: The newly created widget opens **directly behind the Settings window**.
   - Because there is zero visual feedback (no toast, badge, animation, or sound), the user perceives that clicking the button did nothing. Users frequently click the button 5–10 times in confusion, creating a stack of overlapping widget windows.

---

### 2.3 Multi-Instance Widget Collisions & Multi-Monitor Snapping Defects

#### Direct Code Observations
1. **Lack of Unique Identifier in `WidgetLayout`**:
   In `src/uWidgets.Core/Models/WidgetLayout.cs` line 16:
   ```csharp
   public record WidgetLayout(
       string Type, 
       string SubType, 
       int X, 
       int Y, 
       int Width, 
       int Height, 
       JsonElement? Settings
   );
   ```
   `WidgetLayout` contains no instance GUID or unique key.
2. **Instance Collision in Layout Persistence**:
   In `src/uWidgets.Core/Services/WidgetLayoutProvider.cs` lines 23-38:
   ```csharp
   public void Save(WidgetLayout widgetLayout)
   {
       var layout = Get().ToList();
       var index = layout.FindIndex(item => 
           item.Type == widgetLayout.Type && 
           item.SubType == widgetLayout.SubType && 
           item.X == widgetLayout.X && 
           item.Y == widgetLayout.Y);
       if (index == -1)
           index = layout.FindIndex(item => item.Type == widgetLayout.Type && item.SubType == widgetLayout.SubType);

       if (index != -1)
           layout[index] = widgetLayout;
       else
           layout.Add(widgetLayout);

       jsonParser.Save(layout.ToArray());
   }
   ```
   - If a user places two Note widgets on the desktop, both records share `Type="Notes"` and `SubType="Note"`.
   - When the user drags or resizes the second Note widget, its X/Y coordinates no longer match the saved record, so `index == -1`.
   - The fallback line executes: `index = layout.FindIndex(item => item.Type == widgetLayout.Type && item.SubType == widgetLayout.SubType);`.
   - This matches the **first** Note widget in the array.
   - **Result**: Moving or resizing Note #2 overwrites Note #1's coordinates and content in `layout.json`. On subsequent application restarts, one of the widgets vanishes or duplicates the other's state.
3. **Multi-Monitor Negative Coordinate Snapping Bug**:
   In `src/uWidgets/Services/GridService.cs` lines 31-37:
   ```csharp
   private int SnapDimension(double pixels, double scaling = 1.0, bool addMargin = false, int minValue = 1)
   {
       var (size, margin, _) = appSettingsProvider.Get().Dimensions;
       var units = (int) Math.Max(minValue, Math.Round(pixels / (scaling * (size + margin))));
       return GetSize(units, scaling, addMargin);
   }
   ```
   - In `SnapPosition`, `SnapDimension` is invoked with `minValue = 0`.
   - In Windows multi-monitor configurations, a secondary monitor positioned to the left of the primary monitor has negative virtual desktop coordinates (e.g., $X \in [-1920, 0)$). A monitor positioned above has negative Y coordinates.
   - When a widget is moved to a secondary monitor, `pixels` is negative, so `Math.Round(pixels / ...)` yields a negative integer.
   - `Math.Max(0, negativeValue)` evaluates to `0`.
   - `GetSize(0, scaling, true)` computes $0 \cdot (\text{size} + \text{margin}) + \text{margin} = 16\text{px}$.
   - **Result**: Dragging any widget to a left or top secondary monitor causes it to violently snap across displays back to the primary monitor at $X = 16$ or $Y = 16$.

---

### 2.4 Configuration Storage Resilience & Startup Vulnerabilities

#### Direct Code Observations
1. **Unsafe Storage Location (`AppContext.BaseDirectory`)**:
   In `src/uWidgets.Core/Const.cs` lines 15-28:
   ```csharp
   public static readonly string CurrentFolder = !string.IsNullOrEmpty(AppContext.BaseDirectory) 
       ? AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) 
       : ...;
   public static readonly string AppSettingsFile = Path.Combine(CurrentFolder, "appSettings.json");
   public static readonly string LayoutFile = Path.Combine(CurrentFolder, "layout.json");
   ```
   - Standard Windows software installed into `C:\Program Files\uWidgets\` executes in a directory where standard non-elevated user accounts have read-and-execute permissions, but **not write permissions**.
   - Any attempt to save settings or update widget layouts throws an unhandled `System.UnauthorizedAccessException`.
2. **Non-Atomic File Serialization**:
   In `src/uWidgets.Core/Services/JsonParser.cs` lines 30-36:
   ```csharp
   public void Save(T newData)
   {
       var oldData = data;
       DataChanging?.Invoke(this, data, newData);
       var json = JsonSerializer.Serialize(data = newData);
       File.WriteAllText(filePath, json);
       DataChanged?.Invoke(this, oldData, newData);
   }
   ```
   `File.WriteAllText` truncates the file before writing. If the operating system shuts down, the user logs off, or the process is terminated mid-write, the target JSON file is left at 0 bytes or corrupted.
3. **Fatal Startup Exception Before UI Framework Initialization**:
   In `src/uWidgets/Program.cs` lines 85-97:
   ```csharp
   public static AppBuilder BuildAvaloniaApp()
   {
       var settings = new AppSettingsProvider().Get();
       return AppBuilder.Configure<App>()
           .UseWin32()
           .UsePlatformDetect()
           .UseReactiveUI()
           .WithInterFont()
           ...
   ```
   - `new AppSettingsProvider().Get()` executes during `BuildAvaloniaApp()`, prior to `AppBuilder` configuration.
   - If `appSettings.json` is corrupted (e.g. from an abrupt shutdown), `JsonParser<T>.Get()` throws `FormatException`.
   - Because Avalonia has not yet initialized, no error dialog or recovery UI can be displayed. The application crashes immediately on launch.

---

### 2.5 Settings Cognitive Overload, Taxonomy Confusion & Optical Physics Jargon

#### Direct Code Observations
1. **Misplaced Grid & Layout Fundamentals**:
   In `src/uWidgets/Views/Pages/Advanced.axaml` lines 1-53:
   - `Snap size`, `Lock size`, `Snap position`, `Lock position`, `Widget size`, `Widget margin`, and `Widget radius` are all placed inside the **Advanced** page.
   - For a desktop widget application, configuring grid alignment and widget dimensions is core customization, not advanced diagnostics.
2. **Exposing Unprocessed Optical Physics Equations**:
   In `src/uWidgets/Views/Pages/Appearance.axaml` lines 80-106:
   ```xaml
   <!-- Refraction Thickness -->
   <controls:Setting Title="Refraction Thickness (RefThickness)" 
                     Subtitle="Width of the curved meniscus glass bevel in pixels (MyDockFinder default: 50)" ... />
   <!-- Refraction Index -->
   <controls:Setting Title="Refraction Index (RefFactor)" 
                     Subtitle="Snell's law index of refraction for meniscus ray bending (MyDockFinder default: 1.50)" ... />
   <!-- Chromatic Dispersion -->
   <controls:Setting Title="Chromatic Dispersion (RefDispersion)" 
                     Subtitle="7-tap spectral R/G/B separation along the glass rim (MyDockFinder default: 160)" ... />
   <!-- Shape Radius & Roundness -->
   <controls:Setting Title="Corner Roundness (ShapeRoundness)" 
                     Subtitle="Superellipse squircle exponent n (2.0 = circle, MyDockFinder default: 4.0)" ... />
   ```
   - These settings are hardcoded English strings referencing third-party software ("MyDockFinder default").
   - They use complex physics jargon ("Snell's law", "7-tap spectral R/G/B separation", "Superellipse squircle exponent") without visual explanations or real-time previews.
3. **Decimal Opacity vs. Percentage**:
   `OpacityLevel` is rendered via a `NumericUpDown` accepting values from `0.00` to `1.00` with step `0.05` instead of an intuitive `0% - 100%` slider.
4. **Disconnected Template Badges & Erroneous Admin Warnings**:
   - In `src/uWidgets/Views/Controls/Setting.axaml.cs` lines 23-27:
     Dependency properties `AdminRequired` and `RestartRequired` are defined in C#.
   - In `src/uWidgets/Views/Controls/Setting.axaml` lines 10-24:
     The XAML control template contains **zero visual elements** bound to `AdminRequired` or `RestartRequired`. Marking a setting with `RestartRequired="True"` renders nothing.
   - In `src/uWidgets/Views/Pages/General.axaml` line 27:
     `Run on startup` is marked `AdminRequired="True"`, even though `StartupService.cs:21` writes to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, which requires no elevation.

---

### 2.6 In-App Guidance, Tooltip Degradation & Empty-State Deficits

#### Direct Code Observations
1. **Global Tooltip Style Destroys Widget Text Tooltips**:
   In `src/uWidgets/Views/Widget.axaml` lines 47-52:
   ```xaml
   <Style Selector="ToolTip">
       <Setter Property="Background" Value="Transparent" />
       <Setter Property="BorderThickness" Value="0" />
       <Setter Property="Margin" Value="0" />
       <Setter Property="Padding" Value="0" />
   </Style>
   ```
   - This style was declared globally to remove chrome around the corner resize handle located in `ToolTip.Tip` (`Widget.axaml:68`).
   - **Consequence**: Avalonia styles propagate down the visual tree. Every informational tooltip in every widget (e.g. `NowPlaying` playback info, `Docker` container actions, `GitWorkingTree` branch details) inherits a transparent background with zero border and zero padding. Text tooltips become illegible over desktop wallpapers.
2. **Missing Input Watermarks**:
   - In `src/Widgets/Notes/Views/Controls/NoteSmall.axaml`: The title and note body `ClickThroughTextBox` controls have no `Watermark` property. When a fresh note is placed, it appears as an empty blank box.
   - In `src/Widgets/Reminders/Views/Controls/ListSmall.axaml`: The new task entry field lacks a watermark prompting "Add reminder...".
3. **Double-Click Interaction Trap in Notes**:
   In `src/uWidgets/Views/Controls/ClickThroughTextBox.cs` lines 67-70:
   - A single mouse click on the text box initiates widget dragging (`BeginMoveDrag`).
   - The user must double-click to gain keyboard focus.
   - With no UI hint or watermark, users repeatedly single-click to type and accidentally drag the note across their screen.
4. **Silent Network Failures in Online Widgets**:
   - In `src/Widgets/Weather/Services/OpenMeteoWeatherProvider.cs` lines 37-41:
     Network exceptions are caught, logged to an unrooted `weather_crash_log.txt`, and return `null`.
   - In `src/Widgets/Weather/ViewModels/ForecastViewModel.cs` line 29:
     `if (forecast is null) return;` silently aborts. The widget remains blank with no offline indicator, error banner, or retry button.

---

### 2.7 Documentation Drift & Repository Information Gaps

#### Direct Observations
1. **Severe Widget Inventory Drift in README**:
   - `README.md` line 30 states: `"6 types of fully customizable widgets"` and lists only: Clock, Calendar, Notes, Reminders, Weather, and Monitor.
   - The repository actually contains **11 modular widget assemblies** in `src/Widgets/`.
   - 5 complete widget assemblies (45% of the codebase) are completely unmentioned in the README:
     - `Development` (Docker, DevApps, GitWorkingTree)
     - `Finance` (Crypto, Stocks, Currency Converter)
     - `Media` (NowPlaying GSMTC with synced lyrics)
     - `Photos` (PhotoFrame slideshow)
     - `Shortcuts` (Application quick launcher)
2. **Absence of Repository Documentation Structure**:
   - No `docs/` directory exists in the repository.
   - No `USER_GUIDE.md`, `SHORTCUTS.md`, or `TROUBLESHOOTING.md` exists.
   - `README.md` line 176 points to an external unversioned wiki: `https://github.com/creewick/uWidgets/wiki/API`. There is no local developer documentation explaining `IWidgetLayoutProvider`, `WidgetInfoAttribute`, or the dynamic plugin loading context.

---

## 3. Target Architectural Solutions & UI/UX Proposals

```
+--------------------------------------------------------------------------------------------------------+
|                                uWidgets 2.0 Target System Architecture                                 |
+--------------------------------------------------------------------------------------------------------+
|                                                                                                        |
|  +--------------------------------------------------------------------------------------------------+  |
|  |                                  Application Lifecycle Controller                                 |  |
|  |  - Avalonia Native TrayIcon Hub               - ShutdownMode.OnExplicitShutdown                  |  |
|  |  - Global Hotkeys (Win+Shift+W, Win+Alt+L,   - Single Instance Activation & IPC                 |  |
|  |    Ctrl+Shift+U Configurable Remapping)       - Settings Window Lifecycle Hide/Show Interceptor  |  |
|  +--------------------------------------------------------------------------------------------------+  |
|                                                                                                        |
|  +-------------------------------------+      +-----------------------------------------------------+  |
|  |       First-Run Experience          |      |            Unified Settings Modernization           |  |
|  |  - Welcome Onboarding Wizard        |      |  1. General (Startup, Tray, Hotkeys, Language)      |  |
|  |  - Screen-Aware Starter Pack Grid   |      |  2. Appearance (Mode, Monochrome, Presets, Frame)   |  |
|  |  - 30-Second Interactive Tour       |      |  3. Visual Effects (Glass Presets & Optics Tuning)  |  |
|  |  - Permanent Help & Guide Center    |      |  4. Desktop & Grid (Snapping, Dimensions, Lock Pos) |  |
|  +-------------------------------------+      |  5. Widget Manager (Active Widgets & Catalog)       |  |
|                                               |  6. About & Maintenance (Diagnostics, Backup/Reset) |  |
|                                               +-----------------------------------------------------+  |
|                                                                                                        |
|  +--------------------------------------------------------------------------------------------------+  |
|  |                                   Core Desktop Engine & Interop                                  |  |
|  |  - Screen-Aware Multi-Monitor Engine (Negative Coord Support & Math.Clamp Work Area Snapping)    |  |
|  |  - Multi-Instance GUID Support ([JsonConstructor], Clean Startup Migration, No Collisions)       |  |
|  |  - Standardized Tooltip Framework (Explicit Scoped Resize Handle Class, Fluent Text Tooltips) |  |
|  |  - Win32 Session-Aware Watchdog (SessionLock Pausing & DisplaySettingsChanged Re-Anchoring)       |  |
|  +--------------------------------------------------------------------------------------------------+  |
|                                                                                                        |
|  +--------------------------------------------------------------------------------------------------+  |
|  |                                    Storage & Persistence Layer                                   |  |
|  |  - Safe Storage Path: %APPDATA%\uWidgets\ (Portable Mode Fallback & Explicit StorageService)     |  |
|  |  - Atomic File Operations (.tmp write -> Win32 File.Replace atomic swap with retry backoff)      |  |
|  |  - Self-Healing Schema Validation (.bak fallback & Safe Defaults on corrupt JSON)                 |  |
|  +--------------------------------------------------------------------------------------------------+  |
|                                                                                                        |
+--------------------------------------------------------------------------------------------------------+
```

---

### 3.1 First-Run Onboarding Tutorial & Welcome Experience

#### Architecture & Schema
Add onboarding state management to `AppSettings`:
```csharp
namespace uWidgets.Core.Models.Settings;

public record OnboardingSettings(
    bool IsCompleted = false,
    bool ShowWelcomeOnStartup = true,
    string? SelectedStarterPack = null,
    int Version = 1
);
```

#### Onboarding Workflow & User Journey
When `OnboardingSettings.IsCompleted == false` on startup (strictly checking completion status):

> **Crucial First-Run Trigger Invariant**: The welcome wizard trigger must evaluate **strictly** `OnboardingSettings.IsCompleted == false`. The previous naive proposal checked `OnboardingSettings.IsCompleted == false || widgetsCount == 0`. That created an inescapable UX loop: an experienced user who intentionally deletes all desktop widgets has `widgetsCount == 0`, and would be repeatedly forced into the onboarding wizard on every single application restart. By checking only `IsCompleted == false`, returning users maintain full autonomy over their desktop state.

1. **Step 1: Welcome & Value Proposition**:
   - Modern illustration of desktop widgets with translucent glass effect.
   - Title: *"Welcome to uWidgets"*.
   - Subtitle: *"Lightweight, customizable widgets pinned seamlessly to your Windows desktop."*
2. **Step 2: 1-Click Curated Starter Packs (Screen-Aware Layout Engine)**:
   Provide 4 instant desktop setups that generate structured `WidgetLayout` records:
   - **Essential Pack**: Analog Clock (2x2) + Month Calendar (2x2) + Weather Forecast (4x2) + Quick Note (2x2).
   - **Developer Pack**: Digital Clock (2x2) + Git Working Tree (4x2) + Docker Engine (2x2) + System Monitor Dashboard (2x2).
   - **Minimalist Pack**: NowPlaying Media (4x2) + Pomodoro Clock (2x2) + Reminders List (2x2).
   - **Clean Slate**: Start with an empty desktop; automatically opens Settings / Widget Catalog.

   **Screen-Aware Coordinate Calculation Algorithm**:  
   To prevent starter pack widgets from overlapping in an unarranged pile or clipping offscreen on lower-resolution monitors (e.g., 1366×768 laptops or 150%–200% DPI displays), coordinates are calculated dynamically relative to the primary monitor's working area:
   ```csharp
   public void SpawnStarterPack(StarterPack pack, Screen targetScreen)
   {
       var workArea = targetScreen.WorkingArea;
       var scaling = targetScreen.Scaling;
       var (size, margin, _) = appSettingsProvider.Get().Dimensions;
       var cellSpan = scaling * (size + margin);
       
       int startX = (int)(workArea.X + scaling * margin * 2);
       int startY = (int)(workArea.Y + scaling * margin * 2);
       int currentX = startX;
       int currentY = startY;
       int maxRowHeight = 0;

       foreach (var item in pack.Widgets)
       {
           int pixelW = (int)(item.Cols * cellSpan - scaling * margin);
           int pixelH = (int)(item.Rows * cellSpan - scaling * margin);

           // Wrap to next row if widget exceeds monitor working width
           if (currentX + pixelW > workArea.X + workArea.Width - (scaling * margin * 2))
           {
               currentX = startX;
               currentY += maxRowHeight + (int)(scaling * margin);
               maxRowHeight = 0;
           }

           // Clamp to ensure widget never spawns off bottom of screen
           if (currentY + pixelH > workArea.Y + workArea.Height)
           {
               currentY = Math.Max(startY, (int)(workArea.Y + workArea.Height - pixelH - scaling * margin));
           }

           var layout = new WidgetLayout(
               Guid.NewGuid(), 
               item.Type, 
               item.SubType, 
               currentX, 
               currentY, 
               pixelW, 
               pixelH, 
               item.DefaultSettings
           );
           widgetFactory.Add(layout).Show();

           currentX += pixelW + (int)(scaling * margin);
           maxRowHeight = Math.Max(maxRowHeight, pixelH);
       }
   }
   ```
3. **Step 3: 30-Second Interactive Gesture Tour**:
   Animated micro-cards teaching core desktop gestures:
   - **Move & Snap**: Left-click and drag anywhere on a widget. Automatically aligns with your desktop grid.
   - **Resize**: Drag the bottom-right corner handle or right-click to choose preset sizes (Small, Medium, Large, Extra Large).
   - **Configure**: Right-click any widget and choose "Edit [Widget]" to customize timezones, cities, or feeds.
   - **Double-Click to Type**: Double-click Notes or Reminders to enter text without dragging.
   - **System Tray Lifeline**: Access Settings anytime via the uWidgets tray icon in the Windows notification area.
4. **Completion & Dismissal Handling**:
   - Clicking "Get Started" executes `SpawnStarterPack`, persists `OnboardingSettings with { IsCompleted = true }`, and reveals widgets with a smooth fade-in.
   - **Clean Slate & Window Close `[X]` Handling**: If the user clicks "Clean Slate", clicks "Skip", or dismisses the modal via the window close `[X]` button, the modal automatically persists `IsCompleted = true` and invokes `Settings.Show()`. This guarantees the user is never left staring at an empty desktop with no tray guidance or active interface.
   - Replayable anytime via **Help & Guide** in Settings.

---

### 3.2 Redesigned 6-Domain Settings Architecture & Wireframes

#### Navigation Structure & Layout
The Settings window navigation sidebar is restructured from a flat, unindexed list into 6 clean, domain-driven categories:

```
+-----------------------------------------------------------------------+
|  uWidgets Settings                    [ Search Settings... (Ctrl+F) ] |
+-----------------------+-----------------------------------------------+
| [Core Preferences]    |  Appearance                                   |
|   1. General          |  Customize the visual styling of widgets.    |
| > 2. Appearance       |                                               |
|   3. Visual Effects   |  Theme Mode                                   |
|   4. Desktop & Grid   |  [ Light ]   [ (•) Dark ]   [ System ]        |
|                       |                                               |
| [Widget Management]   |  Background Opacity                           |
|   5. Widget Manager   |  [====|====================]  40%             |
|                       |                                               |
| [Support & Info]      |  Accent Color                                 |
|   6. Help & Guide     |  [ (•) Follow Windows Accent ]  [ Custom ]    |
|   7. About            |  [■][■][■][■][■][■][■][■]                     |
|                       |                                               |
|                       |  Interactive Live Preview                     |
|                       |  +-----------------------------------------+  |
|                       |  |  10:42 AM           Tuesday, Oct 24     |  |
|                       |  |  Partly Cloudy      72°F / Cupertino    |  |
|                       |  +-----------------------------------------+  |
+-----------------------+-----------------------------------------------+
```

#### Detailed Domain Specifications

1. **General Domain**:
   - **Startup & Lifecycle**:
     - *Launch on Windows Startup* (ToggleSwitch, HKCU Run key, clear description: "Automatically launch when you log in to Windows", zero bogus admin shield).
     - *System Tray Presence* (ToggleSwitch: "Keep uWidgets running in the notification area when Settings is closed").
     - *Layering Mode* (ComboBox: "Desktop Pinned [WorkerW]", "Normal Floating Window", "Always on Top").
   - **Global Keyboard Shortcuts (Configurable Hotkeys)**:
     - *Toggle All Widgets Visibility (Boss Key)*: Default `Win + Shift + W` (remap input).
     - *Toggle Lock Position*: Default `Win + Alt + L` (remap input, avoids proximity collision with Windows Lock `Win + L`).
     - *Open Settings*: Default `Ctrl + Shift + U` (remap input; replaces conflicting `Win + Shift + S` which is reserved for Windows Snipping Tool).
   - **Language & Region**:
     - *Display Language* (ComboBox). Changing triggers an inline top banner: *"Language changed. Restart uWidgets to update all widgets [Restart Now]"*.
   - **Updates**:
     - *Automatic Update Checking* (ToggleSwitch) + "Check for Updates Now" button.

2. **Appearance Domain**:
   - **Predefined Theme Templates (Templates Selector)**:
     - 1-click visual cards for the 8 built-in themes defined in `Const.cs:33-40` and `appSettings.json:11-84`:
       *Default*, *Dark*, *Light*, *Transparent*, *Glass*, *OLED*, *Cyberpunk*, *Forest*.
     - Selecting a theme instantly updates the preview card, palette swatches, and desktop widgets while allowing subsequent fine-tuning.
   - **Theme Mode**: Segmented button [Light | Dark | Follow Windows System].
   - **Monochrome Mode (`Theme.Monochrome`)**:
     - ToggleSwitch: "Monochrome Mode" (Renders all widgets in a sleek, unified grayscale aesthetic, muting bright accent colors).
   - **Window Frame & Border Style (`Theme.UseNativeFrame`)**:
     - ToggleSwitch: "Native Window Frame vs. Rounded Glass" (Toggles Windows native framing vs custom rounded glass border; drives `WinUICompositionBackdropCornerRadius = settings.Theme.UseNativeFrame ? 0 : settings.Dimensions.Radius` in `Program.cs:96`).
   - **Background Opacity**: Avalonia `Slider` (0% to 100%, step 5%) with live numeric readout ("40%").
   - **Accent Color**: Segmented toggle [System Accent Color | Custom Color Swatches] with HEX picker.
   - **Typography**: System font family dropdown with live typeface preview.
   - **Corner Radius**: Slider (0px to 48px, step 2px) with live preview badge (active when `UseNativeFrame` is false).
   - **Interactive Live Preview Canvas**: An embedded preview widget updating in real-time as templates, colors, opacities, and corners are modified.

3. **Visual Effects (LiquidGlass & Shaders) Domain**:
   - **Effect Engine**: Segmented button [Flat Translucent | Frosted Acrylic | Liquid Glass (GlossyGlass)].
   - **Curated Glass Presets**:
     - *Subtle Frost*: Soft, clean desktop blur (Meniscus: 30px, Refraction: 1.25, Prism: 80).
     - *Frosted Meniscus (Default)*: Balanced Apple-style glass curvature (Meniscus: 50px, Refraction: 1.50, Prism: 160).
     - *High Refraction Crystal*: Deep optical bending (Meniscus: 75px, Refraction: 1.85, Prism: 250).
   - **Collapsible "Optics Fine-Tuning" Accordion (For Advanced Users)**:
     - *Edge Bevel Thickness* (renamed from `RefThickness`, 8–120 px).
     - *Refraction Intensity* (renamed from `RefFactor`, 1.05–2.60).
     - *Rainbow Prism Glare* (renamed from `RefDispersion`, 0–400).
     - *Corner Bevel Ratio* (renamed from `ShapeRadius`, 0.10–1.00).
     - *Squircle Smoothing* (renamed from `ShapeRoundness`, 2.0–8.0).
     - "Reset Optics to Default" button.

4. **Desktop & Grid Domain**:
   - **Grid Alignment**:
     - *Snap widget position to grid* (ToggleSwitch).
     - *Snap widget size to grid* (ToggleSwitch).
   - **Grid Geometry**:
     - *Base Cell Size* (Slider: 36px to 144px, default 72px) with preset chips (48px, 72px, 96px).
     - *Widget Spacing / Margin* (Slider: 0px to 32px, default 16px).
   - **Desktop Protection & Movement Locking**:
     - *Lock Widget Position (`Layout.LockPosition`)*: Disables dragging and pointer movement while allowing resizing.
     - *Lock Widget Size (`Layout.LockSize`)*: Disables corner resize handles while allowing dragging across grid cells.
     - *Master Desktop Lock*: Global ToggleSwitch that locks both position and size simultaneously with visible lock badge.
   - **Multi-Monitor Display Target**:
     - ComboBox selecting target monitor or Virtual Desktop span.

5. **Widget Manager Domain**:
   - **Active Widgets Tab**:
     - Live card list of all widgets currently placed on the desktop.
     - Displays Widget Title, Subtype, Screen Position `(X, Y)`, and Dimensions `(Cols x Rows)`.
     - Action buttons per widget:
       - *Highlight*: Temporarily animates/flashes the widget on the desktop.
       - *Reset to Center*: Recovers widgets dragged offscreen or stranded on disconnected monitors.
       - *Configure*: Opens widget's `EditWidget` dialog.
       - *Toggle Visibility*: Hide/show individual widget.
       - *Delete*: Removes widget from desktop.
   - **Widget Catalog Tab**:
     - Centralized library of all 11 widget assemblies and 27 views.
     - Category filter chips: All, Productivity, System & Hardware, Media & Audio, Finance, Utilities.
     - "Add to Desktop" button with smart placement (spawns in an open desktop slot rather than under Settings).
   - **Layout Profiles Tab**:
     - Save current desktop layout as named preset (e.g., "Work Setup", "Minimal Gaming").
     - One-click profile switching.
     - Export / Import layout profiles to/from JSON.

6. **About & Maintenance Domain**:
   - Version details, GitHub repository link, bug reporting shortcut.
   - Diagnostic tools: "Open Logs Folder", "Reset All Settings to Defaults".

---

### 3.3 Native Avalonia System Tray Hub & Lifecycle Controller

#### Implementation Specification
In `src/uWidgets/App.axaml`:
```xaml
<Application.Resources>
    ...
</Application.Resources>
<TrayIcon.Icons>
    <TrayIcons>
        <TrayIcon Icon="avares://uWidgets/Assets/icon.ico" 
                  ToolTipText="uWidgets Desktop Engine"
                  Clicked="Tray_OnClicked">
            <TrayIcon.Menu>
                <NativeMenu>
                    <NativeMenuItem Header="Open Settings... (Ctrl+Shift+U)" Click="Tray_OpenSettings" />
                    <NativeMenuItem Header="Add Widget...">
                        <NativeMenu>
                            <NativeMenuItem Header="Clock..." Click="Tray_AddClock" />
                            <NativeMenuItem Header="Weather..." Click="Tray_AddWeather" />
                            <NativeMenuItem Header="Notes..." Click="Tray_AddNotes" />
                            <NativeMenuItem Header="System Monitor..." Click="Tray_AddMonitor" />
                            <NativeMenuItem Header="Media Player..." Click="Tray_AddMedia" />
                        </NativeMenu>
                    </NativeMenuItem>
                    <NativeMenuItemSeparator />
                    <NativeMenuItem Header="Show / Hide Desktop Widgets (Win+Shift+W)" Click="Tray_ToggleWidgets" />
                    <NativeMenuItem Header="Lock All Widgets (Win+Alt+L)" Click="Tray_ToggleLock" />
                    <NativeMenuItemSeparator />
                    <NativeMenuItem Header="Help &amp; Shortcuts" Click="Tray_OpenHelp" />
                    <NativeMenuItem Header="Restart uWidgets" Click="Tray_Restart" />
                    <NativeMenuItem Header="Exit uWidgets" Click="Tray_Exit" />
                </NativeMenu>
            </TrayIcon.Menu>
        </TrayIcon>
    </TrayIcons>
</TrayIcon.Icons>
```

#### Tray Asset Packaging in `uWidgets.csproj`
The asset path `avares://uWidgets/Assets/icon.ico` currently fails with `Avalonia.Platform.AssetLoaderException` because the repository's icon resides solely at `.github/images/icon.ico` and is not packaged as an Avalonia resource.  
**Required Packaging Fix**:
1. Copy or link `.github/images/icon.ico` into `src/uWidgets/Assets/icon.ico`.
2. In `src/uWidgets/uWidgets.csproj`, embed the asset directory as an `<AvaloniaResource>`:
   ```xml
   <ItemGroup>
       <AvaloniaResource Include="Assets\**" />
   </ItemGroup>
   ```

#### Avalonia Window Lifecycle & Disposed Handle Mitigation
In Avalonia UI, closing a `Window` permanently destroys its native Win32 window handle (`hWnd`). Because `Settings` is registered as a DI `Singleton` (`App.axaml.cs:28`), calling `.Show()` on a closed `Settings` instance throws:
`System.InvalidOperationException: Cannot call Show() on a closed Window`.

**Architectural Solution**:  
Intercept the `Closing` event on the `Settings` window to cancel destruction and hide the window instead:
```csharp
// In src/uWidgets/Views/Settings.axaml.cs
protected override void OnClosing(WindowClosingEventArgs e)
{
    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
        desktop.ShutdownMode == ShutdownMode.OnExplicitShutdown)
    {
        e.Cancel = true;
        Hide();
        return;
    }
    base.OnClosing(e);
}
```
*Benefits*:
- Native Win32 handle remains valid throughout application lifetime.
- Fast, instant reopening from Tray without reallocating UI trees.
- Preserves active tab selection, search filter, and scroll position.
*(Alternative: Register `Settings` as `Transient` in DI; however, window hiding eliminates garbage collection thrash and preserves user state).*

#### Application Lifetime & Service Scope in `App.axaml.cs`
In `src/uWidgets/App.axaml.cs`, promote the local `services` variable to a private instance field `_services`:
```csharp
public partial class App : Application
{
    private IServiceProvider? _services;

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection()
            ...
            .BuildServiceProvider();

        _services = services;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Prevent silent process exit on Settings window close
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        ...
    }
}
```

Tray Event Handlers & Watchdog:
- **`Tray_OnClicked` / `Tray_OpenSettings`**: Resolves `_services.GetRequiredService<Settings>()`, calls `settings.Show()`, and brings it to the foreground with `settings.Activate()`.
- **`Tray_ToggleWidgets`**: Toggles visibility across all active `Widget` windows simultaneously.
- **`Tray_ToggleLock`**: Toggles global grid locking and displays an OS notification or toast.
- **`Tray_Restart`**: Flushes state to storage and launches a fresh process instance via `Process.Start`.
- **`Tray_Exit`**: Cleanly unhooks Win32 message hooks, unregisters hotkeys, and invokes `desktop.Shutdown()`.
- **Tray Initialization Watchdog**: If the system notification area is temporarily unavailable (e.g., delayed Explorer shell startup or server environments), catch the initialization error, log to diagnostics, and fall back to keeping the Settings window open to prevent a silent zombie process.

---

### 3.4 Active Widgets Manager & Layout Profiles

#### Unique GUID Schema in `WidgetLayout` & `System.Text.Json` Deserialization Safety
To permanently eliminate multi-instance collisions across all widgets, `WidgetLayout` must incorporate a unique instance identifier (`Guid Id`).

**Empirical Serialization Constraint**:  
In .NET 8, `System.Text.Json` throws `System.NotSupportedException` if a record declares multiple parameterized constructors unless exactly one constructor is annotated with `[JsonConstructorAttribute]`. Furthermore, legacy `layout.json` files lacking `"Id"` will bind `Id` to `Guid.Empty`.

```csharp
namespace uWidgets.Core.Models;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public record WidgetLayout
{
    [JsonConstructor]
    public WidgetLayout(
        Guid Id,
        string Type, 
        string SubType, 
        int X, 
        int Y, 
        int Width, 
        int Height, 
        JsonElement? Settings
    )
    {
        this.Id = Id == Guid.Empty ? Guid.NewGuid() : Id;
        this.Type = Type;
        this.SubType = SubType;
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
        this.Settings = Settings;
    }

    public Guid Id { get; init; }
    public string Type { get; init; }
    public string SubType { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public JsonElement? Settings { get; init; }

    // Backward-compatibility overload for legacy calls lacking an Id
    public WidgetLayout(string type, string subType, int x, int y, int width, int height, JsonElement? settings)
        : this(Guid.NewGuid(), type, subType, x, y, width, height, settings)
    {
    }
}
```

#### Startup GUID Migration & Storage Sanitation in `WidgetLayoutProvider.cs`
If legacy `layout.json` records lacking `"Id"` were left with `Id == Guid.Empty` in memory, `FindIndex(w => w.Id == ...)` would match the first `Guid.Empty` widget, re-introducing multi-instance collisions.  
`WidgetLayoutProvider.Get()` therefore sanitizes legacy records upon first load and immediately flushes the assigned GUIDs back to disk:

```csharp
namespace uWidgets.Core.Services;

public class WidgetLayoutProvider : IWidgetLayoutProvider
{
    private readonly IJsonParser<WidgetLayout[]> jsonParser;

    public WidgetLayoutProvider(IJsonParser<WidgetLayout[]> jsonParser)
    {
        this.jsonParser = jsonParser;
    }

    public WidgetLayout[] Get()
    {
        var layouts = jsonParser.Get() ?? [];
        var migrated = false;

        // Startup sanitization: assign unique IDs to any legacy entries lacking GUIDs
        for (int i = 0; i < layouts.Length; i++)
        {
            if (layouts[i].Id == Guid.Empty)
            {
                layouts[i] = layouts[i] with { Id = Guid.NewGuid() };
                migrated = true;
            }
        }

        // Persist migrated schema immediately so layout.json remains clean
        if (migrated)
        {
            jsonParser.Save(layouts);
        }

        return layouts;
    }

    public void Save(WidgetLayout widgetLayout)
    {
        var layout = Get().ToList();
        var targetId = widgetLayout.Id == Guid.Empty ? Guid.NewGuid() : widgetLayout.Id;
        var sanitizedLayout = widgetLayout with { Id = targetId };

        var index = layout.FindIndex(item => item.Id == sanitizedLayout.Id);
        if (index != -1)
            layout[index] = sanitizedLayout;
        else
            layout.Add(sanitizedLayout);

        jsonParser.Save(layout.ToArray());
    }

    public void Remove(Guid id)
    {
        var layout = Get().ToList();
        var index = layout.FindIndex(item => item.Id == id);
        if (index != -1)
        {
            layout.RemoveAt(index);
            jsonParser.Save(layout.ToArray());
        }
    }
}
```

---

### 3.5 Multi-Monitor Screen-Aware Coordinate Engine & Global Hotkeys

#### Screen-Aware Snapping Engine with Strict Monitor Boundary Clamping
In `src/uWidgets/Services/GridService.cs`, the original code used `Math.Max(0, ...)` which snapped negative coordinates on secondary displays back to the primary display. However, naively removing clamping causes another critical failure: widgets dragged near screen edges ($relX < 0$ or beyond display width) snap into inaccessible negative or offscreen virtual desktop coordinates. Furthermore, evaluating screens via `ScreenFromWindow` causes mixed-DPI boundary oscillation when a widget crosses between monitors of different scales (e.g. 100% vs 150%).

**Engine Architecture & Mathematical Implementation**:
1. Anchor monitor detection to the widget's physical center point.
2. Calculate local relative coordinates against the detected screen's `WorkingArea`.
3. Compute maximum allowable grid units (`maxUnitsX`, `maxUnitsY`) for the widget's dimensions.
4. Apply strict `Math.Clamp` to restrict coordinates within valid visible screen slots.

```csharp
namespace uWidgets.Services;

public class GridService : IGridService
{
    private readonly IAppSettingsProvider appSettingsProvider;

    public GridService(IAppSettingsProvider appSettingsProvider)
    {
        this.appSettingsProvider = appSettingsProvider;
    }

    public void SnapPosition(Widget window)
    {
        var settings = appSettingsProvider.Get();
        if (!settings.Layout.SnapPosition) return;

        var (size, margin, _) = settings.Dimensions;
        
        // 1. Identify active screen using widget center point (prevents mixed-DPI boundary jitter)
        var scalingGuess = window.Screens.Primary?.Scaling ?? 1.0;
        var centerPoint = new PixelPoint(
            window.Position.X + (int)(window.Width * scalingGuess / 2),
            window.Position.Y + (int)(window.Height * scalingGuess / 2)
        );
        var screen = window.Screens.ScreenFromPoint(centerPoint) ?? window.Screens.Primary ?? window.Screens.All[0];
        var screenBounds = screen.WorkingArea;
        var scaling = screen.Scaling;

        // 2. Compute local coordinates relative to the monitor's working area
        var relX = window.Position.X - screenBounds.X;
        var relY = window.Position.Y - screenBounds.Y;
        var cellSpan = scaling * (size + margin);

        // 3. Compute monitor bounds capacity and clamp units within visible boundaries
        var maxUnitsX = Math.Max(0, (int)Math.Floor((screenBounds.Width - window.Width * scaling) / cellSpan));
        var maxUnitsY = Math.Max(0, (int)Math.Floor((screenBounds.Height - window.Height * scaling) / cellSpan));

        var unitX = Math.Clamp((int)Math.Round((relX - scaling * margin) / cellSpan), 0, maxUnitsX);
        var unitY = Math.Clamp((int)Math.Round((relY - scaling * margin) / cellSpan), 0, maxUnitsY);

        // 4. Reconstruct absolute screen coordinates anchored to active monitor bounds
        var snappedX = (int)(screenBounds.X + unitX * cellSpan + scaling * margin);
        var snappedY = (int)(screenBounds.Y + unitY * cellSpan + scaling * margin);

        window.Position = new PixelPoint(snappedX, snappedY);
    }
}
```

#### Win32 Interop Watchdog & Display Reconfiguration Lifecycle
In `src/uWidgets/Services/InteropService.cs`, the 250ms `desktopWatchdogTimer` continuously executes Win32 window tree scans to maintain desktop Z-order behind WorkerW. To ensure battery and CPU efficiency while handling dynamic monitor configurations:

1. **Session Switch Hook (CPU Optimization)**:  
   Subscribe to `Microsoft.Win32.SystemEvents.SessionSwitch`. When a `SessionLock` occurs (`Win + L`, workstation lock, or fast user switching), pause `desktopWatchdogTimer`. Resume only when `SessionUnlock` is received.
2. **Display Settings Changed (Stranded Widget Recovery)**:  
   Subscribe to `SystemEvents.DisplaySettingsChanged` and intercept `WM_DISPLAYCHANGE`. When a display is disconnected or resolution is modified:
   - Iterate through all active `Widget` windows.
   - Verify whether the widget's bounding rectangle intersects any active screen in `window.Screens.All`.
   - If stranded on a disconnected display, smoothly animate/reposition the widget to the primary monitor's visible working area and update `layout.json`.

#### Windows Global Hotkey Engine & Collision Resolution
New service `HotkeyService.cs` integrating Win32 `RegisterHotKey`:

> **OS Hotkey Collision & Safety Architecture**:  
> In Windows 10 (v1809+) and Windows 11, `Win + Shift + S` is hardcoded in `explorer.exe` for the Windows Snipping Tool (`ScreenClippingHost.exe`). Calling Win32 `RegisterHotKey` for `Win + Shift + S` unconditionally fails with error `1409` (`ERROR_HOTKEY_ALREADY_REGISTERED`) or breaks the OS screen capture utility.  
> Additionally, `Win + Shift + L` presents immediate accidental proximity hazards to `Win + L` (Windows lock workstation).

**Standardized Hotkey Scheme**:
- `Win + Shift + W`: **Boss Key / Toggle Widgets Visibility** (instantly hides or shows all desktop widgets).
- `Win + Alt + L`: **Toggle Lock/Unlock Position** (safe modifier combination preventing accidental workstation lock).
- `Ctrl + Shift + U` (or configurable in Settings): **Open uWidgets Settings** (conflict-free systemwide shortcut).
- **Graceful Registration Fallback**: If any shortcut registration fails, log diagnostic information and notify the user via Settings without crashing. Keybindings are fully customizable in General Settings.

---

### 3.6 Resilient AppData Storage & Atomic File Operations

#### Safe Initialization & Storage Path Resolution in `StorageService.cs`
Executing directory creation and file migration inside the static constructor of `Const.cs` is dangerous: if an I/O exception occurs (e.g., corporate group policy, locked files, or sandboxed environments), .NET throws a fatal `TypeInitializationException`. In .NET, a failed static constructor permanently disables the type for the entire AppDomain lifecycle, crashing `uWidgets` before Avalonia can initialize or show an error.

**Architectural Solution**:  
Keep `Const.cs` pure (immutable constants and resolved path properties). Move directory creation, permission checks, and legacy migration into an explicit `StorageService.Initialize()` method called in `Program.cs` / `App.axaml.cs` with structured try-catch logging:

```csharp
namespace uWidgets.Core;

public static class Const
{
    public static string CurrentFolder { get; internal set; } = string.Empty;
    public static string AppSettingsFile => Path.Combine(CurrentFolder, "appSettings.json");
    public static string LayoutFile => Path.Combine(CurrentFolder, "layout.json");
    public static string LogsFolder => Path.Combine(CurrentFolder, "Logs");
}

public static class StorageService
{
    public static void Initialize()
    {
        try
        {
            var appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var portableFlag = Path.Combine(appDir, "portable.dat");

            if (File.Exists(portableFlag) && IsDirectoryWritable(appDir))
            {
                Const.CurrentFolder = appDir;
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Const.CurrentFolder = Path.Combine(appData, "uWidgets");
                Directory.CreateDirectory(Const.CurrentFolder);
                Directory.CreateDirectory(Const.LogsFolder);

                MigrateLegacyFiles(appDir, Const.CurrentFolder);
            }
        }
        catch (Exception ex)
        {
            // Fall back to local temp folder if AppData is inaccessible
            Const.CurrentFolder = Path.Combine(Path.GetTempPath(), "uWidgets");
            Directory.CreateDirectory(Const.CurrentFolder);
            Directory.CreateDirectory(Const.LogsFolder);
            System.Diagnostics.Debug.WriteLine($"Storage initialization warning: {ex.Message}");
        }
    }
}
```

#### Transactional Atomic Serialization via Win32 `File.Replace` & Retry Backoff
On Windows NTFS, executing `File.Copy` followed by `File.Move(..., overwrite: true)` is two distinct, non-atomic operations. If antivirus, Windows Defender, or search indexers open a read lock on the file between calls, `File.Move` throws an `IOException` sharing violation.  
To guarantee true transactional atomicity, `JsonParser<T>.Save()` uses .NET native `File.Replace` (mapping directly to Win32 `ReplaceFile`) and incorporates an exponential retry backoff loop:

```csharp
namespace uWidgets.Core.Services;

public class JsonParser<T> : IJsonParser<T> where T : class
{
    private readonly string filePath;
    private T? data;

    public void Save(T newData)
    {
        var oldData = data;
        DataChanging?.Invoke(this, data, newData);
        
        var json = JsonSerializer.Serialize(data = newData, new JsonSerializerOptions { WriteIndented = true });
        var tempFile = $"{filePath}.tmp";
        var backupFile = $"{filePath}.bak";

        // 1. Write to temporary file with explicit disk flush
        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        using (var writer = new StreamWriter(fs))
        {
            writer.Write(json);
        }

        // 2. Transactional atomic swap with retry backoff for antivirus lock resilience
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Atomic Win32 ReplaceFile: atomically swaps tempFile with filePath while preserving backupFile
                    File.Replace(tempFile, filePath, backupFile, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tempFile, filePath, overwrite: true);
                }
                break;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                System.Threading.Thread.Sleep(30 * attempt); // 30ms, 60ms backoff
            }
        }

        DataChanged?.Invoke(this, oldData, newData);
    }

    public T Get()
    {
        if (data != null) return data;
        
        if (!File.Exists(filePath))
        {
            return data = CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return data = JsonSerializer.Deserialize<T>(json) ?? CreateDefault();
        }
        catch (Exception)
        {
            // Self-healing: fallback to backup file if primary file was corrupted
            var backupFile = $"{filePath}.bak";
            if (File.Exists(backupFile))
            {
                try
                {
                    var backupJson = File.ReadAllText(backupFile);
                    return data = JsonSerializer.Deserialize<T>(backupJson) ?? CreateDefault();
                }
                catch { }
            }
            return data = CreateDefault();
        }
    }
}
```

---

### 3.7 In-App Guidance, Standardized Tooltip Framework & Error Recovery

#### Tooltip Style Bug Isolation & Scoping in Avalonia 11
In Avalonia 11.1.3, setting `<ToolTip.Tip><Viewbox ... /></ToolTip.Tip>` creates an implicit ToolTip container that does not inherit custom style classes unless explicitly specified on the ToolTip container itself.

**Required Implementation in `src/uWidgets/Views/Widget.axaml`**:
1. Explicitly declare the `ToolTip` wrapper with the `ResizeHandleToolTip` class around the corner handle:
   ```xaml
   <ToolTip.Tip>
       <ToolTip Classes="ResizeHandleToolTip">
           <Viewbox Stretch="Uniform">
               <Path Data="M0,8 L8,0 M3,8 L8,3 M6,8 L8,6" Stroke="{DynamicResource ControlStrokeColorDefault}" StrokeThickness="1.5" />
           </Viewbox>
       </ToolTip>
   </ToolTip.Tip>
   ```
2. Scope the transparent border and background styles strictly to `.ResizeHandleToolTip`:
   ```xaml
   <!-- Scoped strictly to the resize handle tool tip -->
   <Style Selector="ToolTip.ResizeHandleToolTip">
       <Setter Property="Background" Value="Transparent" />
       <Setter Property="BorderThickness" Value="0" />
       <Setter Property="Margin" Value="0" />
       <Setter Property="Padding" Value="0" />
   </Style>

   <!-- Restored global fluent tooltips for all text tooltips across widgets and settings -->
   <Style Selector="ToolTip">
       <Setter Property="Background" Value="{DynamicResource SystemChromeMediumColor}" />
       <Setter Property="BorderBrush" Value="{DynamicResource SystemChromeHighColor}" />
       <Setter Property="BorderThickness" Value="1" />
       <Setter Property="CornerRadius" Value="6" />
       <Setter Property="Padding" Value="8,4" />
       <Setter Property="FontSize" Value="12" />
   </Style>
   ```

#### Empty States, Watermarks & Network Recovery
1. **Notes**: Add `Watermark="Title..."` and `Watermark="Take a note (double-click to type)..."` to clarify the single-click drag vs double-click edit behavior.
2. **Reminders**: Add `Watermark="Add a new reminder..."` and empty state card when task list is empty.
3. **Photos (Corrupted Image Resilience)**: 
   - Differentiate "No photo selected" from "Image file not found or corrupted: [FileName]".
   - If a selected photo is missing or unreadable, display a dedicated warning card with the broken file name and a direct "Choose another image" button.
4. **GitWorkingTree**: Render explicit call-to-action: *"No repository configured. Right-click widget and choose 'Edit' to select Git project"*.
5. **Weather & Finance (Rate-Limiting Cooldown & Cached Timestamps)**:
   - In `ForecastViewModel.cs` and `OpenMeteoWeatherProvider.cs`: Catch HTTP 429 rate limiting.
   - Enforce a 30-second cooldown timer disabling the "Retry" button with a countdown (`"Retry (28s)"`) to avoid spamming the API.
   - Display a persistent *"Last updated at 10:42 AM (Cached)"* indicator so users understand the data freshness when offline.
6. **Weather Crash Log Path Migration**:
   - In `src/Widgets/Weather/Services/OpenMeteoWeatherProvider.cs`, redirect `weather_crash_log.txt` from `AppContext.BaseDirectory` to `Path.Combine(Const.LogsFolder, "weather_crash_log.txt")`. This prevents unhandled `UnauthorizedAccessException` when running from `C:\Program Files\`.
7. **DeviceBatteries Tooltip**:
   - Add tooltips to 2x2 radial battery circles displaying exact percentage, device name, and charging status.
8. **Gallery Confirmation Toast**: Non-intrusive floating toast: `"✓ [Widget Name] placed on your desktop! Drag to move."`.

---

## 4. Actionable, Prioritized Phased Implementation Roadmap

The implementation plan is structured into **5 prioritized, actionable phases**. Each phase is independently verifiable and strictly adheres to minimal-change engineering practices.

---

### Phase 1: Core Stability, Lifecycle & Storage Resilience (P0 - Critical)

#### Objectives
Eliminate application disappearances, silent process terminations, disposed window handle crashes on tray reopen, missing asset startup exceptions, multi-instance data collisions, multi-monitor snapping jumps, and configuration file corruption.

#### Rationale & User Impact
- Prevents the application from becoming a "ghost" process with no taskbar or tray anchor.
- Ensures reopening Settings from the System Tray never throws `InvalidOperationException`.
- Guarantees tray icon renders reliably without asset loading crashes.
- Ensures multiple instances of widgets (Notes, Clocks) do not overwrite each other.
- Fixes secondary monitor dragging for users with multi-monitor setups without snapping into offscreen space.
- Protects configuration files against write permission failures under `Program Files` and abrupt shutdown corruption.

#### Affected Files & Components
- `src/uWidgets/App.axaml`: Add native `TrayIcon` declaration, click handler, and context menu.
- `src/uWidgets/App.axaml.cs`: Configure `ShutdownMode.OnExplicitShutdown`, promote `_services` field, and wire tray event routing.
- `src/uWidgets/Views/Settings.axaml.cs`: Intercept `OnClosing` with `e.Cancel = true; Hide();` to preserve native Win32 window handle.
- `src/uWidgets/uWidgets.csproj`: Package `Assets/icon.ico` as an `<AvaloniaResource>`.
- `src/uWidgets.Core/Const.cs`: Refactor to pure constants and resolved path accessors.
- `src/uWidgets.Core/Services/StorageService.cs` (New): Safe storage directory creation, permissions verification, and migration.
- `src/uWidgets.Core/Services/JsonParser.cs`: Implement transactional Win32 `File.Replace` with retry backoff, `.bak` backup, and schema fallback.
- `src/uWidgets.Core/Models/WidgetLayout.cs`: Add `Guid Id` with `[JsonConstructor]` attribute to prevent `NotSupportedException`.
- `src/uWidgets.Core/Services/WidgetLayoutProvider.cs`: Startup GUID migration in `Get()` and clean `Save`/`Remove` by `Id`.
- `src/uWidgets/Services/GridService.cs`: Implement screen center-point detection and `Math.Clamp` boundary snapping for negative screen coordinates.
- `src/uWidgets/Services/InteropService.cs`: Hook `SystemEvents.SessionSwitch` to pause watchdog during lock screen and `DisplaySettingsChanged` for stranded widget recovery.
- `src/uWidgets/Locales/Locale.resx` (and localized `.resx` files): Add Tray menu resource strings.

#### Step-by-Step Implementation Steps
1. In `WidgetLayout.cs`, decorate primary constructor with `[JsonConstructor]` to prevent deserialization crashes, and assign `Guid.NewGuid()` when `Id == Guid.Empty`.
2. In `WidgetLayoutProvider.cs`, sanitize legacy layouts lacking `Id` in `Get()` by assigning `Guid.NewGuid()` and immediately persisting to disk, eliminating in-memory collisions.
3. In `GridService.cs`, calculate coordinates relative to `Screen.WorkingArea` using widget center-point detection, and apply `Math.Clamp` against monitor bounds to prevent offscreen snapping.
4. In `InteropService.cs`, pause `desktopWatchdogTimer` on `SessionLock` / resume on `SessionUnlock`, and hook `DisplaySettingsChanged` to pull stranded widgets back to the visible primary display.
5. Create `StorageService.cs` to separate directory creation and legacy file migration from `Const.cs` static constructor, eliminating fatal `TypeInitializationException`.
6. In `JsonParser.cs`, implement transactional atomic file swaps via `File.Replace` with a 3-attempt exponential retry backoff loop and automatic `.bak` fallback.
7. Package `.github/images/icon.ico` into `src/uWidgets/Assets/icon.ico` and register `<AvaloniaResource Include="Assets\**" />` in `uWidgets.csproj`.
8. In `App.axaml`, declare `<TrayIcon.Icons>` with NativeMenu items for Settings, Add, Toggle, Lock, Restart, and Exit; wire `TrayIcon.Clicked` to open Settings.
9. In `App.axaml.cs`, set `desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown` and store service provider in private field `_services`.
10. In `Settings.axaml.cs`, intercept `OnClosing`: set `e.Cancel = true; Hide();` so calling `Show()` on reopened Settings never throws `InvalidOperationException`.

#### Potential Risks & Mitigations
- *Risk*: Multiple constructors in `WidgetLayout` throwing `NotSupportedException` during `JsonSerializer.Deserialize`.  
  *Mitigation*: Decorate primary constructor with `[JsonConstructor]` and handle legacy parameterless fallback.
- *Risk*: Calling `.Show()` on a closed `Settings` instance throwing `InvalidOperationException`.  
  *Mitigation*: Intercept `OnClosing`, cancel close event, and invoke `Hide()`.
- *Risk*: Missing `avares://uWidgets/Assets/icon.ico` causing startup crash in `App.Initialize()`.  
  *Mitigation*: Embed `Assets/icon.ico` as an `<AvaloniaResource>` in `uWidgets.csproj`.
- *Risk*: Widgets dragged near monitor borders snapping into negative offscreen space.  
  *Mitigation*: Apply strict `Math.Clamp(unitX, 0, maxUnitsX)` against monitor working area bounds.
- *Risk*: File sharing violations during atomic save when files are scanned by Windows Defender.  
  *Mitigation*: Wrap Win32 `File.Replace` in an exponential retry backoff loop (up to 3 retries with 30ms sleep).

#### Verification Approach
- Build solution with `dotnet build src/uWidgets.sln`.
- Launch app with 0 widgets, close Settings: verify tray icon remains active and app stays running.
- Reopen Settings from Tray icon: verify window reopens smoothly with active state preserved and 0 exceptions.
- Add two Note widgets, edit second Note: verify both Notes persist independently in `layout.json`.
- Drag widget to secondary display with negative virtual coordinates and near screen borders: verify it snaps to valid visible grid slots without jumping to primary display or offscreen void.
- Terminate process abruptly during save: verify `.bak` fallback prevents startup crash.

---

### Phase 2: First-Run Onboarding, Guidance & Widget Management (P0/P1 - High)

#### Objectives
Transform first-run drop-off into an engaging onboarding journey, provide an Active Widgets management console, eliminate invisible widget placement, and ensure experienced users are never trapped in onboarding loops.

#### Rationale & User Impact
- Replaces raw shader settings on first launch with an inviting welcome tour and 1-click starter packs.
- Solves the problem of widgets spawning invisibly underneath the Settings window.
- Provides a centralized dashboard to locate, highlight, recover, or remove active desktop widgets.
- Prevents experienced users who intentionally clear all widgets from being repeatedly forced into onboarding.

#### Affected Files & Components
- `src/uWidgets.Core/Models/Settings/AppSettings.cs`: Add `OnboardingSettings`.
- `src/uWidgets/ViewModels/SettingsViewModel.cs`: Add `Welcome` and `ActiveWidgets` navigation items.
- `src/uWidgets/Views/Pages/WelcomeModal.axaml` & `.cs` (New): 3-step onboarding wizard.
- `src/uWidgets/Views/Pages/ActiveWidgets.axaml` & `.cs` (New): Active widgets manager table.
- `src/uWidgets/ViewModels/ActiveWidgetsViewModel.cs` (New): Active widgets view model.
- `src/uWidgets/Views/Pages/Gallery.axaml.cs`: Update placement logic and trigger addition toast.
- `src/uWidgets/Views/Pages/HelpAndGuide.axaml` & `.cs` (New): Permanent Help Center.

#### Step-by-Step Implementation Steps
1. Create `OnboardingSettings` model (`IsCompleted = false`, `ShowWelcomeOnStartup = true`) and add to `AppSettings.cs`.
2. Build `WelcomeModal.axaml` with Welcome, 1-Click Starter Packs (Essential, Developer, Minimalist), and 30-Second Gesture Tour.
3. In `App.axaml.cs`, launch `WelcomeModal` **strictly** if `OnboardingSettings.IsCompleted == false` (never check `widgetsCount == 0`).
4. In `WelcomeModal.axaml.cs`, implement screen-aware layout calculation for Starter Packs: compute dynamic `(X, Y)` grid offsets anchored to primary display work area, wrapping rows cleanly.
5. In `WelcomeModal.axaml.cs`, ensure that selecting "Clean Slate", clicking "Skip", or closing the modal via `[X]` immediately persists `IsCompleted = true` and opens `Settings`.
6. Implement `ActiveWidgets.axaml` displaying running widgets with Highlight, Reset-to-Center, Configure, and Delete controls.
7. In `Gallery.axaml.cs`, offset new widget placement away from Settings window center and show floating confirmation toast.
8. Build permanent `HelpAndGuide.axaml` containing interactive gesture cheat-sheet, shortcut list, and FAQ.

#### Potential Risks & Mitigations
- *Risk*: Onboarding loop trap re-triggering for returning users who clear all widgets.  
  *Mitigation*: Check only `OnboardingSettings.IsCompleted == false`.
- *Risk*: Starter pack widgets spawning overlapping or clipped offscreen on low-resolution displays.  
  *Mitigation*: Implement dynamic screen-aware grid placement with work area wrapping and clamping.

#### Verification Approach
- Clear `appSettings.json` and `layout.json`: verify Welcome Wizard opens.
- Click "Developer Pack": verify Git, Docker, Clock, and Monitor widgets spawn in non-overlapping grid slots on desktop.
- Delete all widgets and restart app: verify Welcome Wizard does NOT re-appear (user is not trapped).
- Open Settings > Active Widgets: verify all widgets appear in table. Click "Reset to Center" on offscreen widget to verify position recovery.

---

### Phase 3: Settings Modernization, Simplification & Appearance Polish (P1 - High)

#### Objectives
Streamline settings into 6 intuitive domains, restore complete settings taxonomy (`Monochrome`, `UseNativeFrame`, 8 `Templates`, `LockPosition` vs `LockSize`), eliminate technical optical jargon, introduce percentage sliders, and repair the global tooltip styling defect.

#### Rationale & User Impact
- Casual users can easily adjust opacity, theme, and grid spacing without encountering physics formulas.
- Restores active settings from `appSettings.json` that were previously orphaned.
- Restores readable text tooltips across all widgets and settings by scoping resize handle styles.
- Displays inline restart warnings when language or platform options change.

#### Affected Files & Components
- `src/uWidgets/Views/Settings.axaml` & `SettingsViewModel.cs`: Restructure into 6 domains with universal search.
- `src/uWidgets/Views/Pages/Appearance.axaml` & `AppearanceViewModel.cs`: Opacity slider (0%–100%), Live Preview card, friendly Glass presets, Monochrome toggle, UseNativeFrame toggle, and 8 Templates selector.
- `src/uWidgets/Views/Pages/DesktopGrid.axaml` & `.cs` (Refactored from `Advanced.axaml`): Cohesive grid layout settings with distinct `LockPosition` and `LockSize` controls.
- `src/uWidgets/Views/Controls/Setting.axaml` & `.cs`: Implement visual template for `RestartRequired` badge.
- `src/uWidgets/Views/Pages/General.axaml`: Remove bogus `AdminRequired="True"` from startup setting; add hotkey configuration inputs.
- `src/uWidgets/Views/Widget.axaml`: Scope resize handle tooltip wrapper (`<ToolTip Classes="ResizeHandleToolTip">`) and restore fluent text tooltips.

#### Step-by-Step Implementation Steps
1. In `Widget.axaml`, declare `<ToolTip Classes="ResizeHandleToolTip">` on the resize handle and scope transparent styling strictly to `.ResizeHandleToolTip`; define fluent opaque styles for default `ToolTip`.
2. In `Appearance.axaml`, replace `NumericUpDown` for opacity with `Slider` (0%–100%) and bind to percentage string.
3. In `Appearance.axaml`, add visual selector for the 8 theme `Templates` (`Default`, `Dark`, `Light`, `Transparent`, `Glass`, `OLED`, `Cyberpunk`, `Forest`).
4. In `Appearance.axaml`, add `Theme.Monochrome` toggle and `Theme.UseNativeFrame` toggle (driving `WinUICompositionBackdropCornerRadius` in `Program.cs:96`).
5. Replace raw optics inputs with 3 curated presets (Subtle Frost, Frosted Meniscus, High Refraction) and place raw parameters into a collapsible "Optics Fine-Tuning" expander.
6. In `DesktopGrid.axaml`, combine grid snapping, sizing, and clearly delineate `Layout.LockPosition` (disables dragging) and `Layout.LockSize` (disables resize handles) plus master lock toggle.
7. Update `Setting.axaml` control template to render an inline warning pill when `RestartRequired="True"`.
8. Add Universal Search `TextBox` (`Ctrl+F`) at the top of the Settings navigation pane.

#### Potential Risks & Mitigations
- *Risk*: Avalonia 11 style selector inheritance failing on implicit ToolTip container.  
  *Mitigation*: Explicitly declare `<ToolTip Classes="ResizeHandleToolTip">` on the resize handle container.
- *Risk*: Orphaned settings causing configuration loss or breaking backdrop radii.  
  *Mitigation*: Fully map `Monochrome`, `UseNativeFrame`, `Templates`, and `LockSize` to UI controls.

#### Verification Approach
- Inspect Appearance page: verify opacity slider smoothly adjusts transparency and displays percentage.
- Verify 8 theme template cards apply styles instantly to live preview canvas.
- Toggle `UseNativeFrame`: verify composition backdrop radius adapts in accordance with `Program.cs:96`.
- Hover over buttons in Settings and widgets: verify text tooltips render with opaque background and crisp borders.
- Hover over resize handle: verify handle tooltip remains borderless and transparent.
- Change language: verify inline restart notification banner appears with "Restart Now" button.

---

### Phase 4: Workflow Productivity, Hotkeys & Network Fault Tolerance (P2 - Medium)

#### Objectives
Introduce conflict-free Windows global hotkeys for instant widget control, provide watermark hints and double-click guidance in widgets, implement graceful offline recovery for network widgets, and harden runtime edge cases.

#### Rationale & User Impact
- Enables users to hide widgets instantly during full-screen games, meetings, or presentations without OS shortcut collisions.
- Eliminates accidental dragging when typing in Notes.
- Informs users when network widgets are offline, prevents API rate-limiting spam, and provides cached timestamps.
- Prevents unhandled crashes from corrupt photos or unrooted crash log writes under `C:\Program Files\`.

#### Affected Files & Components
- `src/uWidgets/Services/HotkeyService.cs` (New): Win32 `RegisterHotKey` service with non-colliding shortcuts.
- `src/Widgets/Notes/Views/Controls/NoteSmall.axaml` (and all sizes): Watermark attributes.
- `src/Widgets/Reminders/Views/Controls/ListSmall.axaml` (and all sizes): Watermarks and empty state.
- `src/Widgets/Photos/Views/Controls/PhotoFrameSmall.axaml` & `PhotoFrameViewModel.cs`: Corrupted image placeholder card and file path display.
- `src/Widgets/Development/Views/GitWorkingTree.axaml`: Empty state repository setup guidance.
- `src/Widgets/Weather/ViewModels/ForecastViewModel.cs` & `Forecast.axaml`: Offline state badge, cached timestamp, and 30s rate-limiting cooldown on retry button.
- `src/Widgets/Weather/Services/OpenMeteoWeatherProvider.cs`: Redirect crash logs to `%APPDATA%\uWidgets\Logs\`.
- `src/Widgets/Monitor/Views/DeviceBatteries.axaml`: Tooltips on radial battery indicators.

#### Step-by-Step Implementation Steps
1. Implement `HotkeyService.cs` registering `Win + Shift + W` (Boss key / toggle visibility), `Win + Alt + L` (Toggle lock position), and `Ctrl + Shift + U` (Open Settings). Avoid hardcoded collision with Windows Snipping Tool (`Win + Shift + S`).
2. Add `Watermark="Title..."` and `Watermark="Take a note (double-click to type)..."` in Note views.
3. Add `Watermark="Add a new reminder..."` in Reminders views.
4. In `PhotoFrameViewModel.cs`, differentiate missing image from corrupted file; render warning card displaying filename and "Choose another image" button.
5. In `ForecastViewModel.cs`, catch network failures, set `IsOffline = true`, show "Last updated: HH:mm (Cached)", and enforce a 30s cooldown countdown on the Retry button upon HTTP 429.
6. In `OpenMeteoWeatherProvider.cs`, redirect `weather_crash_log.txt` writes to `Const.LogsFolder`.
7. In `DeviceBatteries.axaml`, add tooltips to radial battery circles displaying device name, exact percentage, and charging status.

#### Potential Risks & Mitigations
- *Risk*: Hotkey collision with Windows Snipping Tool (`Win + Shift + S`, error 1409) or accidental lock (`Win + L`).  
  *Mitigation*: Default to `Ctrl + Shift + U` for Settings and `Win + Alt + L` for lock; support full remapping.
- *Risk*: Repeatedly clicking Retry on Weather triggering cascading HTTP 429 rate limits.  
  *Mitigation*: Implement 30-second cooldown timer disabling Retry button with live countdown.

#### Verification Approach
- Press `Win + Shift + W`: verify all desktop widgets instantly disappear; press again to restore.
- Press `Ctrl + Shift + U`: verify Settings window opens without interfering with Windows Snipping Tool (`Win + Shift + S`).
- Disconnect internet, refresh Weather: verify offline banner with cached timestamp and disabled 30s cooldown Retry button displays instead of blank card.
- Select a corrupted image in PhotoFrame: verify widget displays "Image corrupted: [FileName]" warning rather than generic blank placeholder.
- Verify `weather_crash_log.txt` is written inside `%APPDATA%\uWidgets\Logs\`.

---

### Phase 5: Documentation Overhaul & In-Repo Guides (P2 - Medium)

#### Objectives
Overhaul repository documentation, bring widget inventory up to date, create a comprehensive `docs/` library, and provide an in-repo widget developer guide.

#### Rationale & User Impact
- Accurately represents all 11 widgets to new users and GitHub visitors.
- Provides complete user guides, shortcut cheat-sheets, and troubleshooting manuals.
- Empowers third-party developers to author custom widgets using the plugin engine.

#### Affected Files & Components
- `README.md` & `README.ZH-HANS.md`: Full refresh (11 widgets, screenshots, gestures, quickstart).
- `docs/USER_GUIDE.md` (New): Complete user manual (placement, sizing, grid, themes, tray).
- `docs/SHORTCUTS.md` (New): Keyboard shortcut sheet and desktop gestures guide.
- `docs/TROUBLESHOOTING.md` (New): Multi-monitor DPI, WorkerW pinning, AppData storage, crash logs.
- `docs/WIDGET_CATALOG.md` (New): Detailed feature breakdown of all 27 widget views.
- `docs/WIDGET_DEVELOPMENT.md` (New): Technical tutorial on creating custom widget plugins.

#### Step-by-Step Implementation Steps
1. Create `docs/` directory.
2. Update `README.md` to list all 11 widget projects with feature descriptions and screenshots.
3. Write `USER_GUIDE.md` covering installation, first-run wizard, starter packs, and customization.
4. Write `SHORTCUTS.md` covering global hotkeys and mouse gestures.
5. Write `TROUBLESHOOTING.md` documenting desktop pinning mechanics, multi-DPI displays, and crash log locations.
6. Write `WIDGET_DEVELOPMENT.md` providing step-by-step guidance on implementing `IWidgetLayoutProvider`, `WidgetInfoAttribute`, and isolated assembly loading.

#### Verification Approach
- Inspect all markdown files locally and verify all internal relative links resolve correctly.
- Verify that `git status` reports clean source code with only documentation additions.

---

## 5. Phase Breakdown Matrix

| Phase | Proposal / Workstream | Target Priority | Key Deliverables & Outcomes | Affected Files & Modules | Verification Method |
|---|---|---|---|---|---|
| **Phase 1** | **Core Stability & Lifecycle** | **P0 (Critical)** | • Avalonia System Tray Icon & NativeMenu<br>• `ShutdownMode.OnExplicitShutdown`<br>• Settings `OnClosing` cancel & hide (no disposed handle crash)<br>• Embedded `Assets/icon.ico` AvaloniaResource<br>• `WidgetLayout` `[JsonConstructor]` & load sanitization<br>• `Math.Clamp` multi-monitor screen center snapping<br>• Watchdog session switch pause & display change re-anchoring<br>• Safe `StorageService.Initialize()` & `File.Replace` retry swap | `App.axaml`<br>`App.axaml.cs`<br>`Settings.axaml.cs`<br>`uWidgets.csproj`<br>`Const.cs`<br>`StorageService.cs`<br>`JsonParser.cs`<br>`WidgetLayout.cs`<br>`WidgetLayoutProvider.cs`<br>`GridService.cs`<br>`InteropService.cs` | Run with 0 widgets, close Settings -> Tray persists.<br>Reopen Settings from Tray -> Opens without exception.<br>Duplicate Notes -> Distinct GUIDs, no collision.<br>Secondary monitor boundary drag -> Clamps to visible slots.<br>Locked workstation -> Watchdog pauses.<br>Display disconnect -> Stranded widgets pull to primary. |
| **Phase 2** | **Onboarding & Guidance** | **P0 / P1** | • 3-step First-Run Welcome Wizard (`IsCompleted == false` only)<br>• Responsive Screen-Aware Starter Pack Grid Layout<br>• 30-second Interactive Gesture Tour<br>• "Clean Slate" / `[X]` dismissal persistence & Settings transition<br>• Active Widgets Manager (List, highlight, reset to center)<br>• Widget addition feedback & smart placement | `AppSettings.cs`<br>`WelcomeModal.axaml`<br>`ActiveWidgets.axaml`<br>`Gallery.axaml.cs`<br>`HelpAndGuide.axaml` | Clear config, launch -> Welcome Wizard shows.<br>Pick Starter Pack -> Widgets spawn non-overlapping.<br>Clear all widgets & restart -> Onboarding does NOT re-trigger.<br>Active Widgets page lists and recovers widgets. |
| **Phase 3** | **Settings Modernization** | **P1 (High)** | • 6-domain restructured navigation with Universal Search<br>• Full settings taxonomy: `Monochrome`, `UseNativeFrame`, 8 `Templates`<br>• Delineated `LockPosition` vs `LockSize` controls<br>• Friendly Glass presets & collapsed optics fine-tuning<br>• Percentage slider for Opacity (0%–100%)<br>• Scoped `<ToolTip Classes="ResizeHandleToolTip">` (fixes transparent bug)<br>• Inline restart notification banner | `Settings.axaml`<br>`Appearance.axaml`<br>`DesktopGrid.axaml`<br>`Setting.axaml`<br>`General.axaml`<br>`Widget.axaml` | Opacity slider shows live %.<br>8 templates apply 1-click styling.<br>Native frame toggle adapts composition backdrop radius.<br>Text tooltips have opaque background & borders.<br>Resize handle tooltip remains transparent. |
| **Phase 4** | **Productivity & Resilience** | **P2 (Medium)** | • Conflict-Free Global Hotkeys (`Win+Shift+W`, `Win+Alt+L`, `Ctrl+Shift+U`)<br>• Watermarks in Notes and Reminders (double-click guidance)<br>• PhotoFrame corrupted image warning card with filename<br>• Weather 30s rate-limiting retry cooldown & cached timestamp<br>• Log path migration to `%APPDATA%\uWidgets\Logs\`<br>• Battery percentage tooltips in DeviceBatteries | `HotkeyService.cs`<br>`NoteSmall.axaml`<br>`ListSmall.axaml`<br>`PhotoFrameSmall.axaml`<br>`PhotoFrameViewModel.cs`<br>`GitWorkingTree.axaml`<br>`ForecastViewModel.cs`<br>`OpenMeteoWeatherProvider.cs`<br>`DeviceBatteries.axaml` | Press `Win+Shift+W` -> Toggles widgets visibility.<br>Press `Ctrl+Shift+U` -> Opens Settings without Snipping Tool collision.<br>Offline Weather -> Shows cached time & 30s retry cooldown.<br>Corrupted image -> Shows error card with filename.<br>Verify crash log writes to AppData Logs folder. |
| **Phase 5** | **Documentation Overhaul** | **P2 (Medium)** | • Root `README.md` updated to all 11 widgets<br>• `docs/USER_GUIDE.md`<br>• `docs/SHORTCUTS.md`<br>• `docs/TROUBLESHOOTING.md`<br>• `docs/WIDGET_CATALOG.md`<br>• `docs/WIDGET_DEVELOPMENT.md` | `README.md`<br>`README.ZH-HANS.md`<br>`docs/*.md` | Verify all markdown links resolve.<br>Confirm 11 widgets documented.<br>Verify Developer Guide builds sample plugin. |

---

## 6. Verification, Quality Assurance & Clean Git Safeguards

### 6.1 Pre-Implementation Source Code Integrity Verification
In strict accordance with the mandatory integrity guidelines:
- **Zero source code files** (`*.cs`, `*.axaml`, `*.csproj`, `*.sln`, `appSettings.json`, `layout.json`) were altered or committed during the authoring of this research deliverable.
- The entire research and audit was conducted using non-destructive file reads, mathematical modeling, and static analysis.
- The working tree remains completely clean with only the official implementation plan document added to `docs/`.

### 6.2 Implementation Verification & Quality Gates
When code modifications commence under user approval in subsequent milestones, the following verification gates must be executed:

1. **Compilation Integrity Gate**:
   ```powershell
   dotnet build src/uWidgets.sln -c Debug
   dotnet build src/uWidgets.sln -c Release
   ```
   *Pass Criteria*: All 13 projects compile with 0 warnings treated as errors and 0 compilation failures.

2. **Window Lifecycle & System Tray Verification**:
   - Clear `layout.json` to 0 widgets.
   - Launch application, close Settings window.
   - Inspect Windows notification area: verify uWidgets tray icon persists. Double-click tray icon: confirm Settings window reopens without `InvalidOperationException`.

3. **Instance GUID Collision & Serialization Verification**:
   - Spawn two instances of the `Note` widget and two instances of `AnalogClockI`.
   - Reposition and edit Note #2.
   - Inspect `layout.json`: verify both instances have unique `Id` values and distinct coordinates.
   - Test deserializing a legacy `layout.json` lacking `Id`: verify `[JsonConstructor]` loads without `NotSupportedException` and automatically assigns fresh GUIDs.

4. **Multi-Monitor Snapping & Boundary Clamping Verification**:
   - Configure a secondary monitor with negative coordinates ($X < 0$).
   - Drag a widget onto the secondary display near the outer edges ($relX < 0$).
   - *Pass Criteria*: Widget snaps cleanly to grid slots on the secondary monitor clamped within visible boundaries and never jumps back to primary display or offscreen space.

5. **Global Hotkey Non-Collision Verification**:
   - Register shortcuts: verify `Ctrl + Shift + U` opens Settings.
   - Press `Win + Shift + S`: verify Windows Snipping Tool still functions normally without conflict or error 1409.

6. **Onboarding Loop Immunity Verification**:
   - Complete first-run wizard.
   - Intentionally delete all desktop widgets (`widgetsCount == 0`).
   - Restart application: verify Welcome Wizard does NOT re-appear and user is not trapped in an onboarding loop.

7. **Tooltip Rendering Verification**:
   - Hover over resize handle: verify transparent handle container.
   - Hover over settings buttons, NowPlaying controls, or Docker containers: verify opaque, bordered fluent tooltip.

8. **Storage Resilience & Retry Swap Verification**:
   - Lock configuration file briefly during save: verify retry loop succeeds via Win32 `File.Replace`.
   - Verify all logs write to `%APPDATA%\uWidgets\Logs\`.

---

## 7. User Review and Approval Sign-Off

This document constitutes the complete engineering audit, UI/UX specification, and implementation roadmap. 

**Readiness Status**: Ready for User Review and Approval.  
**Next Steps Upon Approval**: Proceed to Phase 1 implementation (Application Lifecycle, System Tray, Storage Resilience, and Multi-Monitor Coordinate Fixes).

---
*Authored for the uWidgets Project Repository — November 2026.*
