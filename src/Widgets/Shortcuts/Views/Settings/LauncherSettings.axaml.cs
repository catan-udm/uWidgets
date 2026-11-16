using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Shortcuts.Services;
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

    private void Add_OnClick(object? sender, RoutedEventArgs e)
    {
        viewModel.StatusMessage = null;
        viewModel.AddShortcut();
    }

    private void SelectDiscoveredApp_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: DiscoveredApp app })
        {
            viewModel.StatusMessage = null;
            viewModel.AddShortcutFromDiscoveredApp(app);
        }
    }

    private async void BrowseApp_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = await PickFileAsync();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.StatusMessage = null;
            var name = System.IO.Path.GetFileNameWithoutExtension(selected);
            viewModel.AddShortcutFromDiscoveredApp(new DiscoveredApp(name, selected, "App"));
        }
    }

    private async void BrowseFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var selected = await PickFolderAsync();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.StatusMessage = null;
            var name = System.IO.Path.GetFileName(selected.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(name)) name = selected;
            viewModel.AddShortcutFromDiscoveredApp(new DiscoveredApp(name, selected, "Folder"));
        }
    }

    private async void BrowseRowFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: EditableShortcutViewModel item }) return;
        var selected = await PickFileAsync();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.StatusMessage = null;
            item.Target = selected;
        }
    }

    private async void BrowseRowFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: EditableShortcutViewModel item }) return;
        var selected = await PickFolderAsync();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.StatusMessage = null;
            item.Target = selected;
        }
    }

    private void MoveUp_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: EditableShortcutViewModel item })
        {
            viewModel.MoveUp(item);
        }
    }

    private void MoveDown_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: EditableShortcutViewModel item })
        {
            viewModel.MoveDown(item);
        }
    }

    private void DismissStatus_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.StatusMessage = null;

    private async System.Threading.Tasks.Task<string?> PickFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Application or File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Applications & Shortcuts (*.exe, *.lnk, *.bat, *.cmd)")
                {
                    Patterns = ["*.exe", "*.lnk", "*.bat", "*.cmd"]
                },
                new FilePickerFileType("All Files (*.*)")
                {
                    Patterns = ["*.*"]
                }
            ]
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private async System.Threading.Tasks.Task<string?> PickFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

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
            viewModel.StatusMessage = null;
            var customOpt = ShortcutIcon.AddCustomIcon(selected);
            var popup = (sender as Control)?.FindLogicalAncestorOfType<Popup>() ?? (sender as Control)?.FindAncestorOfType<Popup>();
            var hostButton = popup?.PlacementTarget as DropDownButton;

            if (customOpt != null)
            {
                if (hostButton?.DataContext is EditableShortcutViewModel itemVm)
                {
                    itemVm.SelectedIcon = customOpt;
                }
                hostButton?.Flyout?.Hide();
            }
            else
            {
                hostButton?.Flyout?.Hide();
                viewModel.StatusMessage = "Could not load icon. SVG files must contain <path d=\"...\"> geometry; raster images support PNG, ICO, JPG, WebP, or BMP.";
            }
        }
    }

    private void DeleteCustomIcon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string key })
        {
            ShortcutIcon.DeleteCustomIcon(key);
            foreach (var item in viewModel.Items.Where(i => string.Equals(i.IconKey, key, System.StringComparison.OrdinalIgnoreCase)))
            {
                item.IconKey = "App";
            }
        }
    }
}
