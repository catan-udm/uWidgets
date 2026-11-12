using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Services;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class WidgetManager : UserControl
{
    private readonly WidgetManagerViewModel viewModel;
    private readonly ILayoutProvider layoutProvider;
    private readonly WidgetRegistry widgetRegistry;

    public WidgetManager(
        ILayoutProvider layoutProvider,
        IAssemblyProvider assemblyProvider,
        IWidgetFactory<Window, UserControl> widgetFactory,
        WidgetRegistry widgetRegistry,
        WidgetPlacementService placementService)
    {
        this.layoutProvider = layoutProvider;
        this.widgetRegistry = widgetRegistry;
        viewModel = new WidgetManagerViewModel(layoutProvider, assemblyProvider, widgetFactory, widgetRegistry, placementService);
        DataContext = viewModel;
        widgetRegistry.WidgetsChanged += OnWidgetsChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnWidgetsChanged(object? sender, EventArgs e) => viewModel.RefreshActiveWidgets();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        widgetRegistry.WidgetsChanged -= OnWidgetsChanged;
        Unloaded -= OnUnloaded;
    }

    private void RecoverOffscreen_OnClick(object? sender, RoutedEventArgs e) => viewModel.RecoverAllOffscreen();
    private void ToggleAll_OnClick(object? sender, RoutedEventArgs e) => viewModel.ToggleAllVisibility();

    private async void HighlightWidget_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ActiveWidgetItemViewModel item && item.WindowRef != null)
            await item.WindowRef.HighlightAsync();
    }

    private void CenterWidget_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ActiveWidgetItemViewModel item && item.WindowRef != null)
        {
            item.WindowRef.ResetToCenter();
            viewModel.RefreshActiveWidgets();
        }
    }

    private void ToggleWidgetVisibility_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ActiveWidgetItemViewModel item && item.WindowRef != null)
        {
            if (item.WindowRef.IsVisible)
                item.WindowRef.Hide();
            else
                item.WindowRef.Show();
            viewModel.RefreshActiveWidgets();
        }
    }

    private void EditWidget_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ActiveWidgetItemViewModel item && item.WindowRef != null)
            item.WindowRef.EditWidget();
    }

    private void RemoveWidget_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ActiveWidgetItemViewModel item && item.WindowRef != null)
        {
            item.WindowRef.Remove();
            viewModel.RefreshActiveWidgets();
        }
    }

    private void AddCatalogWidget_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is CatalogWidgetItemViewModel item)
            viewModel.AddCatalogWidget(item, VisualRoot as Window);
    }

    private void SaveProfile_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.SaveProfile(viewModel.ProfileNameInput);

    private void ApplyProfile_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is LayoutProfileItemViewModel profile)
            viewModel.ApplyProfile(profile);
    }

    private void DeleteProfile_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is LayoutProfileItemViewModel profile)
            viewModel.DeleteProfile(profile);
    }

    private async void ExportLayout_OnClick(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export uWidgets Layout",
            SuggestedFileName = "uwidgets-layout.json",
            DefaultExtension = "json"
        });
        if (file == null) return;

        await using var stream = await file.OpenWriteAsync();
        await JsonSerializer.SerializeAsync(stream, layoutProvider.Get(), new JsonSerializerOptions { WriteIndented = true });
        viewModel.StatusMessage = $"✓ Exported layout to {file.Name}.";
    }

    private async void ImportLayout_OnClick(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage == null) return;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import uWidgets Layout",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON Files") { Patterns = ["*.json"] }]
        });
        if (files.Count == 0) return;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var loaded = JsonSerializer.Deserialize<List<WidgetLayout>>(json);
        if (loaded == null) return;

        viewModel.ApplyLayoutList(loaded);
        viewModel.StatusMessage = $"✓ Imported {loaded.Count} widget(s) from {files[0].Name}.";
    }
}
