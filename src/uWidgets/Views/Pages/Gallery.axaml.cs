using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Models.Attributes;
using uWidgets.Services;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class Gallery : UserControl
{
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly ILayoutProvider layoutProvider;
    private readonly IAssemblyProvider assemblyProvider;
    private readonly AssemblyInfo assemblyInfo;
    private readonly IWidgetFactory<Window, UserControl> widgetFactory;
    private readonly WidgetPlacementService? placementService;
    private int toastSequence;

    public List<WidgetPreviewViewModel> Widgets => GetWidgets();
    public int WidgetSize => 160;
    public CornerRadius Radius => new(appSettingsProvider.Get().Dimensions.Radius / (VisualRoot?.RenderScaling ?? 1.0));

    public Gallery(IAppSettingsProvider appSettingsProvider, ILayoutProvider layoutProvider, IAssemblyProvider assemblyProvider, 
        AssemblyInfo assemblyInfo, IWidgetFactory<Window, UserControl> widgetFactory, WidgetPlacementService? placementService = null)
    {
        this.appSettingsProvider = appSettingsProvider;
        this.layoutProvider = layoutProvider;
        this.assemblyProvider = assemblyProvider;
        this.assemblyInfo = assemblyInfo;
        this.widgetFactory = widgetFactory;
        this.placementService = placementService;
        DataContext = this;
        Unloaded += OnUnloaded;
        
        InitializeComponent();
    }

    private List<WidgetPreviewViewModel> GetWidgets()
    {
        var assembly = assemblyProvider
            .LoadAssembly(assemblyInfo.AssemblyName);

        var locale = assemblyProvider.GetLocaleResourceManager(assembly);

        var widgets =  assembly
            .GetCustomAttributes<WidgetInfoAttribute>()
            .Select(widgetInfo => new WidgetPreviewViewModel(
                widgetFactory.CreateControl(widgetInfo.ViewType),
                assemblyInfo.AssemblyName,
                widgetInfo.ViewType.Name,
                locale?.GetString(widgetInfo.Title ?? string.Empty),
                locale?.GetString(widgetInfo.Subtitle ?? string.Empty)
            ))
            .ToList();

        return widgets;
    }
    
    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (layoutProvider.Get().All(x => x.Type != assemblyInfo.AssemblyName))
        {
            assemblyProvider.UnloadAssembly(assemblyInfo.AssemblyName);
        }
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var viewModel = button?.DataContext as WidgetPreviewViewModel;
        if (viewModel == null) return;

        var hostWindow = VisualRoot as Window;
        WidgetLayout widgetSettings;

        if (placementService != null && hostWindow != null)
        {
            var avoidRect = WidgetPlacementService.GetWindowPixelBounds(hostWindow);
            widgetSettings = placementService.CreateSmartLayout(hostWindow, viewModel.Type, viewModel.Subtype, 2, 2, avoidRect);
        }
        else
        {
            var dimensions = appSettingsProvider.Get().Dimensions;
            var size = 2 * dimensions.Size + dimensions.Margin;
            var position = button!.PointToScreen(new Point(0, 0));
            widgetSettings = new WidgetLayout(viewModel.Type, viewModel.Subtype, position.X, position.Y, size, size, null);
        }

        widgetFactory.Add(widgetSettings).Show();

        var name = string.IsNullOrWhiteSpace(viewModel.Title) ? viewModel.Subtype : viewModel.Title;
        await ShowToastAsync($"✓ {name} placed on your desktop! Drag to move, or right-click to resize or edit.");
    }

    private async Task ShowToastAsync(string message)
    {
        var currentSeq = ++toastSequence;
        ToastMessage.Text = message;
        ToastBanner.IsVisible = true;
        await Task.Delay(4000);
        if (toastSequence == currentSeq)
            ToastBanner.IsVisible = false;
    }
}