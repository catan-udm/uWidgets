using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Development.ViewModels;
using uWidgets.Core.Interfaces;

namespace Development.Views.Settings;

public partial class GitWorkingTreeSettings : UserControl
{
    private readonly GitWorkingTreeSettingsViewModel? viewModel;

    public GitWorkingTreeSettings() : this(null!)
    {
    }

    public GitWorkingTreeSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        if (widgetLayoutProvider != null)
        {
            viewModel = new GitWorkingTreeSettingsViewModel(widgetLayoutProvider);
            DataContext = viewModel;
        }
        InitializeComponent();
    }

    private async void BrowseFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        if (viewModel == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Git Repository or Working Tree Folder",
            AllowMultiple = false
        });

        var selected = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.AddRepository(selected);
        }
    }

    private async void BrowseExecutable_OnClick(object? sender, RoutedEventArgs e)
    {
        if (viewModel == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Code Editor Executable",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Executable Files (*.exe, *.cmd, *.bat)")
                {
                    Patterns = ["*.exe", "*.cmd", "*.bat"]
                }
            ]
        });

        var selected = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(selected))
        {
            viewModel.CustomEditorCommand = $"\"{selected}\"";
        }
    }
}
