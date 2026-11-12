using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Locales;
using uWidgets.Services;
using uWidgets.Views.Pages;

namespace uWidgets.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly IAssemblyProvider assemblyProvider;
    private readonly ILayoutProvider layoutProvider;
    private readonly IWidgetFactory<Window, UserControl> widgetFactory;
    private readonly IServiceProvider serviceProvider;

    private UserControl? currentPage;
    private string? currentPageTitle;
    private string searchQuery = string.Empty;

    public SettingsViewModel(
        IAppSettingsProvider appSettingsProvider,
        IAssemblyProvider assemblyProvider,
        ILayoutProvider layoutProvider,
        IWidgetFactory<Window, UserControl> widgetFactory,
        IServiceProvider serviceProvider)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.assemblyProvider = assemblyProvider;
        this.layoutProvider = layoutProvider;
        this.widgetFactory = widgetFactory;
        this.serviceProvider = serviceProvider;

        widgetItems = assemblyProvider
            .GetAssemblyInfos(Const.WidgetsFolder)
            .ToDictionary(
                group => group.Key,
                group => group.MaxBy(assembly => assembly.Version)!)
            .Select(assembly => new PageViewModel(
                typeof(Gallery),
                StreamGeometry.Parse(assembly.Value.IconData),
                assembly.Value.DisplayName,
                assembly.Value,
                assembly.Value.AssemblyName))
            .OrderBy(page => page.Text)
            .ToArray();

        AllItems = MenuItems.Concat(widgetItems).ToArray();
        RefreshFilteredItems();
    }

    public UserControl? CurrentPage
    {
        get => currentPage;
        set => this.RaiseAndSetIfChanged(ref currentPage, value);
    }

    public string? CurrentPageTitle
    {
        get => currentPageTitle;
        set => this.RaiseAndSetIfChanged(ref currentPageTitle, value);
    }

    public string SearchQuery
    {
        get => searchQuery;
        set
        {
            this.RaiseAndSetIfChanged(ref searchQuery, value);
            RefreshFilteredItems();
        }
    }

    public ObservableCollection<PageViewModel> FilteredItems { get; } = [];
    public PageViewModel[] AllItems { get; }

    public static readonly PageViewModel[] MenuItems =
    [
        new PageViewModel(typeof(General), GetIcon(nameof(General)), Locale.Settings_General, null, "startup tray hotkey shortcut language layering update"),
        new PageViewModel(typeof(Appearance), GetIcon(nameof(Appearance)), Locale.Settings_Appearance, null, "theme dark light opacity accent monochrome font radius frame preview"),
        new PageViewModel(typeof(VisualEffects), GetIcon("LiquidGlass"), "Visual Effects", null, "liquid glass blur frost refraction dispersion optics shader"),
        new PageViewModel(typeof(DesktopGrid), GetIcon("SnapPosition"), "Desktop & Grid", null, "grid snap lock position size margin spacing dimensions"),
        new PageViewModel(typeof(WidgetManager), GetIcon("WidgetSize"), "Widget Manager", null, "active widgets catalog profiles export import recover offscreen"),
        new PageViewModel(typeof(HelpAndGuide), GetIcon("Play"), "Help & Guide", null, "tutorial welcome tour shortcuts gestures faq logs data folder"),
        new PageViewModel(typeof(About), GetIcon(nameof(About)), Locale.Settings_About, null, "version github bug reset logs data"),
        new PageViewModel(null, null, null)
    ];

    private readonly PageViewModel[] widgetItems;

    private static StreamGeometry? GetIcon(string name) =>
        (StreamGeometry?)(Application.Current!.TryFindResource(name, out var icon) ? icon : null);

    private void RefreshFilteredItems()
    {
        FilteredItems.Clear();
        var query = searchQuery.Trim();

        if (string.IsNullOrEmpty(query))
        {
            foreach (var item in AllItems)
                FilteredItems.Add(item);
            return;
        }

        foreach (var item in AllItems)
        {
            if (item.IsSeparator) continue;
            if ((item.Text?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                item.SearchKeywords.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredItems.Add(item);
            }
        }
    }

    public void SetCurrentPage(PageViewModel? value)
    {
        if (value?.Type == null) return;

        if (value.AssemblyInfo != null)
        {
            var placementService = serviceProvider.GetService<WidgetPlacementService>();
            CurrentPage = new Gallery(appSettingsProvider, layoutProvider, assemblyProvider, value.AssemblyInfo, widgetFactory, placementService);
        }
        else
        {
            CurrentPage = (UserControl)ActivatorUtilities.CreateInstance(serviceProvider, value.Type);
        }

        CurrentPageTitle = value.Text;
    }
}