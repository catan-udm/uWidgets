using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Photos.ViewModels;
using uWidgets.Core.Interfaces;

namespace Photos.Views.Settings;

public partial class PhotoFrameSettings : UserControl
{
    private readonly PhotoFrameSettingsViewModel viewModel;

    public PhotoFrameSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        viewModel = new PhotoFrameSettingsViewModel(widgetLayoutProvider);
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void BrowseFile_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });

        var selected = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.Path = selected;
        }
    }

    private async void BrowseFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false
        });

        var selected = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.Path = selected;
        }
    }
}
