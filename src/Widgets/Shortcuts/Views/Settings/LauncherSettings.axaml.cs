using Avalonia.Controls;
using Avalonia.Interactivity;
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
}
