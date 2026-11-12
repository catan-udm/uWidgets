using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using ReactiveUI;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Models.Attributes;
using uWidgets.Services;
using uWidgets.Views;

namespace uWidgets.ViewModels;

public record ActiveWidgetItemViewModel(
    Guid Id,
    string Type,
    string SubType,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsVisible,
    bool CanEdit,
    Widget? WindowRef)
{
    public string DisplayTitle => $"{Type} — {SubType}";
    public string CoordinatesText => $"Position: ({X}, {Y})  •  Size: {Width}×{Height}px";
    public string VisibilityLabel => IsVisible ? "Hide" : "Show";
}

public record CatalogWidgetItemViewModel(
    string AssemblyName,
    string ModuleDisplayName,
    string SubType,
    string Title,
    string Subtitle,
    string Category);

public record LayoutProfileItemViewModel(
    string Name,
    string FilePath,
    int WidgetCount,
    DateTime LastModified);

public class WidgetManagerViewModel : ReactiveObject
{
    private readonly ILayoutProvider layoutProvider;
    private readonly IAssemblyProvider assemblyProvider;
    private readonly IWidgetFactory<Window, UserControl> widgetFactory;
    private readonly WidgetRegistry widgetRegistry;
    private readonly WidgetPlacementService placementService;

    private string catalogSearch = string.Empty;
    private string selectedCategory = "All";
    private string profileNameInput = string.Empty;
    private string statusMessage = string.Empty;

    public ObservableCollection<ActiveWidgetItemViewModel> ActiveWidgets { get; } = [];
    public ObservableCollection<CatalogWidgetItemViewModel> FilteredCatalog { get; } = [];
    public ObservableCollection<LayoutProfileItemViewModel> Profiles { get; } = [];

    private readonly List<CatalogWidgetItemViewModel> allCatalogItems = [];

    public string[] Categories { get; } = ["All", "Productivity", "System", "Media", "Finance", "Utilities"];

    public string CatalogSearch
    {
        get => catalogSearch;
        set
        {
            this.RaiseAndSetIfChanged(ref catalogSearch, value);
            ApplyCatalogFilter();
        }
    }

    public string SelectedCategory
    {
        get => selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref selectedCategory, value);
            ApplyCatalogFilter();
        }
    }

    public string ProfileNameInput
    {
        get => profileNameInput;
        set => this.RaiseAndSetIfChanged(ref profileNameInput, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref statusMessage, value);
            this.RaisePropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(statusMessage);
    public bool HasNoActiveWidgets => ActiveWidgets.Count == 0;
    public string ActiveWidgetsSummary => $"{ActiveWidgets.Count} widget(s) currently placed on your desktop";

    public WidgetManagerViewModel(
        ILayoutProvider layoutProvider,
        IAssemblyProvider assemblyProvider,
        IWidgetFactory<Window, UserControl> widgetFactory,
        WidgetRegistry widgetRegistry,
        WidgetPlacementService placementService)
    {
        this.layoutProvider = layoutProvider;
        this.assemblyProvider = assemblyProvider;
        this.widgetFactory = widgetFactory;
        this.widgetRegistry = widgetRegistry;
        this.placementService = placementService;

        RefreshActiveWidgets();
        LoadCatalog();
        RefreshProfiles();
    }

    public void RefreshActiveWidgets()
    {
        ActiveWidgets.Clear();
        var layouts = layoutProvider.Get();
        foreach (var layout in layouts)
        {
            var win = widgetRegistry.FindById(layout.Id);
            ActiveWidgets.Add(new ActiveWidgetItemViewModel(
                layout.Id,
                layout.Type,
                layout.SubType,
                win?.Position.X ?? layout.X,
                win?.Position.Y ?? layout.Y,
                win != null ? (int)win.Width : layout.Width,
                win != null ? (int)win.Height : layout.Height,
                win?.IsVisible ?? true,
                win?.ShowEditButton ?? false,
                win));
        }

        this.RaisePropertyChanged(nameof(HasNoActiveWidgets));
        this.RaisePropertyChanged(nameof(ActiveWidgetsSummary));
    }

    public void AddCatalogWidget(CatalogWidgetItemViewModel item, Window? hostWindow)
    {
        var (cols, rows) = GetRecommendedGridSize(item.AssemblyName, item.SubType);
        WidgetLayout layout;
        if (hostWindow != null)
        {
            var avoidRect = WidgetPlacementService.GetWindowPixelBounds(hostWindow);
            layout = placementService.CreateSmartLayout(hostWindow, item.AssemblyName, item.SubType, cols, rows, avoidRect);
        }
        else
        {
            layout = new WidgetLayout(Guid.NewGuid(), item.AssemblyName, item.SubType, 64, 64, 160, 160, null);
        }

        widgetFactory.Add(layout).Show();
        RefreshActiveWidgets();
        StatusMessage = $"✓ Added {item.Title} to your desktop.";
    }

    public void RecoverAllOffscreen()
    {
        foreach (var win in widgetRegistry.ActiveWidgets)
            win.RecoverIfOffscreen();

        RefreshActiveWidgets();
        StatusMessage = "Checked all active widgets and recovered any offscreen placements.";
    }

    public void ToggleAllVisibility()
    {
        var hidden = widgetRegistry.ToggleAllVisibility();
        RefreshActiveWidgets();
        StatusMessage = hidden ? "All desktop widgets hidden." : "All desktop widgets restored.";
    }

    public void SaveProfile(string name)
    {
        var safeName = string.Join("_", name.Trim().Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeName))
        {
            StatusMessage = "Please enter a valid profile name.";
            return;
        }

        Directory.CreateDirectory(Const.ProfilesFolder);
        var path = Path.Combine(Const.ProfilesFolder, $"{safeName}.json");
        var json = JsonSerializer.Serialize(layoutProvider.Get(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        ProfileNameInput = string.Empty;
        RefreshProfiles();
        StatusMessage = $"✓ Saved layout profile \"{safeName}\".";
    }

    public void ApplyProfile(LayoutProfileItemViewModel profile)
    {
        if (!File.Exists(profile.FilePath)) return;

        var json = File.ReadAllText(profile.FilePath);
        var loaded = JsonSerializer.Deserialize<List<WidgetLayout>>(json) ?? [];

        ApplyLayoutList(loaded);
        StatusMessage = $"✓ Loaded profile \"{profile.Name}\" ({loaded.Count} widgets).";
    }

    public void ApplyLayoutList(List<WidgetLayout> loaded)
    {
        widgetRegistry.CloseAll();
        var sanitized = loaded.Select(w => w.Id == Guid.Empty ? w with { Id = Guid.NewGuid() } : w).ToList();
        layoutProvider.Save(sanitized);

        foreach (var widget in widgetFactory.Create())
            widget.Show();

        RefreshActiveWidgets();
    }

    public void DeleteProfile(LayoutProfileItemViewModel profile)
    {
        if (File.Exists(profile.FilePath))
            File.Delete(profile.FilePath);
        RefreshProfiles();
        StatusMessage = $"Deleted profile \"{profile.Name}\".";
    }

    public void RefreshProfiles()
    {
        Profiles.Clear();
        if (!Directory.Exists(Const.ProfilesFolder)) return;

        foreach (var file in Directory.GetFiles(Const.ProfilesFolder, "*.json").OrderByDescending(File.GetLastWriteTime))
        {
            try
            {
                var info = new FileInfo(file);
                var count = (JsonSerializer.Deserialize<List<WidgetLayout>>(File.ReadAllText(file)) ?? []).Count;
                Profiles.Add(new LayoutProfileItemViewModel(
                    Path.GetFileNameWithoutExtension(file),
                    file,
                    count,
                    info.LastWriteTime));
            }
            catch
            {
                // Ignore malformed profile files
            }
        }
    }

    private void LoadCatalog()
    {
        allCatalogItems.Clear();

        var assemblies = assemblyProvider
            .GetAssemblyInfos(Const.WidgetsFolder)
            .ToDictionary(g => g.Key, g => g.MaxBy(a => a.Version)!);

        foreach (var pair in assemblies.OrderBy(p => p.Value.DisplayName))
        {
            var info = pair.Value;
            try
            {
                var asm = assemblyProvider.LoadAssembly(info.AssemblyName);
                var locale = assemblyProvider.GetLocaleResourceManager(asm);
                var category = MapCategory(info.AssemblyName);

                foreach (var attr in asm.GetCustomAttributes<WidgetInfoAttribute>())
                {
                    var title = locale?.GetString(attr.Title ?? string.Empty) ?? attr.ViewType.Name;
                    var subtitle = locale?.GetString(attr.Subtitle ?? string.Empty) ?? info.DisplayName;
                    allCatalogItems.Add(new CatalogWidgetItemViewModel(
                        info.AssemblyName,
                        info.DisplayName,
                        attr.ViewType.Name,
                        title,
                        subtitle,
                        category));
                }
            }
            catch
            {
                // Skip unreadable plugin assembly
            }
        }

        ApplyCatalogFilter();
    }

    private void ApplyCatalogFilter()
    {
        FilteredCatalog.Clear();
        var query = catalogSearch.Trim();

        foreach (var item in allCatalogItems)
        {
            if (selectedCategory != "All" && !string.Equals(item.Category, selectedCategory, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(query) &&
                !item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !item.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !item.ModuleDisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;

            FilteredCatalog.Add(item);
        }
    }

    private static string MapCategory(string assemblyName) => assemblyName switch
    {
        "Calendar" or "Clock" or "Notes" or "Reminders" => "Productivity",
        "Development" or "Monitor" => "System",
        "Media" or "Photos" => "Media",
        "Finance" => "Finance",
        _ => "Utilities"
    };

    private static (int Cols, int Rows) GetRecommendedGridSize(string type, string subType) => (type, subType) switch
    {
        ("Weather", "Forecast") or ("Media", "NowPlaying") or ("Development", "GitWorkingTree") => (4, 2),
        _ => (2, 2)
    };
}
