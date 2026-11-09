using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

public partial class LauncherWide : UserControl
{
    public LauncherWide()
    {
        InitializeComponent();
    }

    public LauncherWide(LauncherViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Shortcut_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as Launcher)?.Shortcut_OnClick(sender, e);
}
