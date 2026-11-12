# uWidgets Widget Development Guide

uWidgets loads widget modules dynamically from the `Widgets/` folder beside the executable (`Const.WidgetsFolder`). Each widget assembly is a standard **.NET 8 Avalonia** class library referencing `uWidgets.Core` and `uWidgets`.

---

## 1. Project Setup

1. Create a .NET 8 class library under `src/Widgets/<YourModuleName>/`.
2. Add project references to:
   - `src/uWidgets.Core/uWidgets.Core.csproj`
   - `src/uWidgets/uWidgets.csproj` (for shared controls such as `ClickThroughTextBox` and `TimerService` if needed)
3. Enable compiled Avalonia XAML (`Avalonia` package).

---

## 2. Registering Widgets with Attributes

In `AssemblyInfo.cs`, declare module metadata using `LocaleAttribute` and one or more `WidgetInfoAttribute` attributes (`src/uWidgets.Core/Models/Attributes/`):

```csharp
using MyWidget.Locales;
using MyWidget.Models;
using MyWidget.Views;
using MyWidget.Views.Settings;
using uWidgets.Core.Models.Attributes;

[assembly: Locale(
    typeof(Locale),
    displayName: "MyWidget_CategoryName",
    iconData: "M12,2 ...")]

[assembly: WidgetInfo(
    viewType: typeof(SampleView),
    modelType: typeof(SampleModel),
    editModelViewType: typeof(SampleSettings),
    title: "MyWidget_Sample_Title",
    subtitle: "MyWidget_Sample_Subtitle")]
```

- `viewType`: `UserControl` rendered inside the desktop `Widget` window.
- `modelType`: Optional record/class serialized into `layout.json` and passed to your view and settings constructors.
- `editModelViewType`: Optional `UserControl` shown inside the **Edit Widget** dialog.
- `title` / `subtitle`: Resource keys resolved against your `Locale` `.resx` file.

---

## 3. View, Model, and Disposal Conventions

### Constructor Convention
Provide both a parameterless design-time constructor and a model constructor:

```csharp
public partial class SampleView : UserControl
{
    public SampleView() : this(new SampleModel()) { }

    public SampleView(SampleModel model)
    {
        DataContext = new SampleViewModel(model);
        Unloaded += (_, _) => ((SampleViewModel)DataContext).Dispose();
        InitializeComponent();
    }
}
```

### Resource Cleanup
Always unsubscribe from `TimerService` events, OS watchers, or timers when `Unloaded` / `Dispose()` runs so removing a widget does not leak memory or background work.

### Persisting Mutable Data & Logs
- Read widget plugin assets relative to the assembly location or `Const.WidgetsFolder`.
- Store **any** user configuration, caches, or diagnostic logs under `Const.CurrentFolder` / `Const.LogsFolder` — never write into `AppContext.BaseDirectory`, which may be read-only (e.g., `Program Files`).

### Interactive Text Inputs
Use `uWidgets.Views.Controls.ClickThroughTextBox` when your widget needs inline editable text on the desktop while still allowing widget dragging around the control.

---

## 4. Building and Testing Locally

Build the full solution to compile your new widget assembly and deploy it into the host output directory:

```powershell
dotnet build src/uWidgets.sln
```

Once built, your DLL is discovered automatically by `AssemblyProvider` on startup and appears in **Settings -> Widget Manager** and the **Sidebar Gallery**.
