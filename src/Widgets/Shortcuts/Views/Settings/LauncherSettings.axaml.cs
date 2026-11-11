using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Shortcuts.ViewModels;
using uWidgets.Core.Interfaces;

namespace Shortcuts.Views.Settings;

public partial class LauncherSettings : UserControl
{
    private readonly LauncherSettingsViewModel viewModel;

    public LauncherSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        viewModel = new LauncherSettingsViewModel(widgetLayoutProvider);
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Add_OnClick(object? sender, RoutedEventArgs e) => viewModel.AddShortcut();

    private void Remove_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: EditableShortcutViewModel item })
        {
            viewModel.RemoveShortcut(item);
        }
    }

    private void SelectIcon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ShortcutIconOption option } button)
        {
            var popup = button.FindLogicalAncestorOfType<Popup>() ?? button.FindAncestorOfType<Popup>();
            if (popup?.PlacementTarget is DropDownButton hostButton &&
                hostButton.DataContext is EditableShortcutViewModel itemVm)
            {
                itemVm.SelectedIcon = option;
                hostButton.Flyout?.Hide();
            }
        }
    }

    private async void UploadCustomIcon_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Icon and Image Files (*.svg, *.png, *.ico, *.jpg, *.webp)")
                {
                    Patterns = ["*.svg", "*.png", "*.ico", "*.jpg", "*.jpeg", "*.webp", "*.bmp"]
                }
            ]
        });

        var selected = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(selected))
        {
            var customOpt = ShortcutIcon.AddCustomIcon(selected);
            if (customOpt != null)
            {
                var popup = (sender as Control)?.FindLogicalAncestorOfType<Popup>() ?? (sender as Control)?.FindAncestorOfType<Popup>();
                if (popup?.PlacementTarget is DropDownButton hostButton &&
                    hostButton.DataContext is EditableShortcutViewModel itemVm)
                {
                    itemVm.SelectedIcon = customOpt;
                    hostButton.Flyout?.Hide();
                }
            }
        }
    }

    private void DeleteCustomIcon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string key })
        {
            ShortcutIcon.DeleteCustomIcon(key);
        }
    }
}
