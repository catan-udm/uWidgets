using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models.Attributes;
using uWidgets.Locales;
using uWidgets.Services;
using uWidgets.Views.Pages;

namespace uWidgets.ViewModels;

public class SettingsViewModel : ReactiveObject, IDisposable
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly IAssemblyProvider assemblyProvider;
    private readonly ILayoutProvider layoutProvider;
    private readonly IWidgetFactory<Window, UserControl> widgetFactory;
    private readonly IServiceProvider serviceProvider;

    private UserControl? currentPage;
    private string? currentPageTitle;
    private string searchQuery = string.Empty;
    private PageViewModel? selectedMenuItem;

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
            .Select(assembly =>
            {
                var subKeywords = !string.IsNullOrWhiteSpace(assembly.Value.SearchKeywords)
                    ? assembly.Value.SearchKeywords
                    : BuildWidgetSearchKeywords(assembly.Value.AssemblyName);
                return new PageViewModel(
                    typeof(Gallery),
                    StreamGeometry.Parse(assembly.Value.IconData),
                    assembly.Value.DisplayName,
                    assembly.Value,
                    subKeywords);
            })
            .OrderBy(page => page.Text)
            .ToArray();

        MenuItems =
        [
            new PageViewModel(typeof(General), GetIcon(nameof(General)), Locale.Settings_General, null, "startup tray hotkey shortcut language layering update"),
            new PageViewModel(typeof(Appearance), GetIcon(nameof(Appearance)), Locale.Settings_Appearance, null, "theme dark light opacity accent monochrome font radius frame preview"),
            new PageViewModel(typeof(VisualEffects), GetIcon("LiquidGlass"), Locale.Settings_VisualEffects, null, "liquid glass blur frost refraction dispersion optics shader"),
            new PageViewModel(typeof(DesktopGrid), GetIcon("SnapPosition"), Locale.Settings_DesktopGrid, null, "grid snap lock position size margin spacing dimensions"),
            new PageViewModel(typeof(WidgetManager), GetIcon("WidgetSize"), Locale.Settings_WidgetManager, null, "active widgets catalog profiles export import recover offscreen"),
            new PageViewModel(typeof(HelpAndGuide), GetIcon("Play"), Locale.Settings_HelpAndGuide, null, "tutorial welcome tour shortcuts gestures faq logs data folder"),
            new PageViewModel(typeof(About), GetIcon(nameof(About)), Locale.Settings_About, null, "version github bug reset logs data"),
            new PageViewModel(null, null, null)
        ];

        AllItems = MenuItems.Concat(widgetItems).ToArray();
        RefreshFilteredItems();
        SelectedMenuItem = MenuItems[0];
    }

    private string BuildWidgetSearchKeywords(string assemblyName)
    {
        try
        {
            var asm = assemblyProvider.LoadAssembly(assemblyName);
            var locale = assemblyProvider.GetLocaleResourceManager(asm);
            var tokens = asm.GetCustomAttributes<WidgetInfoAttribute>()
                .SelectMany(attr => new[]
                {
                    attr.ViewType.Name,
                    locale?.GetString(attr.Title ?? string.Empty),
                    locale?.GetString(attr.Subtitle ?? string.Empty)
                })
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" ", tokens.Prepend(assemblyName));
        }
        catch
        {
            return assemblyName;
        }
    }

    public PageViewModel? SelectedMenuItem
    {
        get => selectedMenuItem;
        set
        {
            if (value == null || ReferenceEquals(selectedMenuItem, value)) return;
            this.RaiseAndSetIfChanged(ref selectedMenuItem, value);
            SetCurrentPage(value);
        }
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
    public PageViewModel[] MenuItems { get; }

    private readonly PageViewModel[] widgetItems;

    private static StreamGeometry? GetIcon(string name) =>
        (StreamGeometry?)(Application.Current!.TryFindResource(name, out var icon) ? icon : null);

    private void RefreshFilteredItems()
    {
        var active = selectedMenuItem;
        FilteredItems.Clear();
        var query = searchQuery.Trim();

        if (string.IsNullOrEmpty(query))
        {
            foreach (var item in AllItems)
                FilteredItems.Add(item);
        }
        else
        {
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

        if (active != null && FilteredItems.Contains(active))
        {
            selectedMenuItem = active;
            this.RaisePropertyChanged(nameof(SelectedMenuItem));
        }
    }

    public void SetCurrentPage(PageViewModel? value)
    {
        if (value?.Type == null) return;

        var previousPage = currentPage;
        UserControl nextPage;
        if (value.AssemblyInfo != null)
        {
            var placementService = serviceProvider.GetService<WidgetPlacementService>();
            nextPage = new Gallery(appSettingsProvider, layoutProvider, assemblyProvider, value.AssemblyInfo, widgetFactory, placementService);
        }
        else
        {
            nextPage = (UserControl)ActivatorUtilities.CreateInstance(serviceProvider, value.Type);
        }

        CurrentPage = nextPage;
        CurrentPageTitle = value.Text;
        DisposePage(previousPage);
    }

    public void Dispose()
    {
        DisposePage(currentPage);
        currentPage = null;
        GC.SuppressFinalize(this);
    }

    private static void DisposePage(UserControl? page)
    {
        if (page == null) return;
        if (page.DataContext is IDisposable vmDisposable && !ReferenceEquals(vmDisposable, page))
            vmDisposable.Dispose();
        if (page is IDisposable pageDisposable)
            pageDisposable.Dispose();
    }
}