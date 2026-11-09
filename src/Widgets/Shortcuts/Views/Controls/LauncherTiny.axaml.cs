using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

public partial class LauncherTiny : UserControl
{
    public LauncherTiny()
    {
        InitializeComponent();
    }

    public LauncherTiny(LauncherViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Shortcut_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as Launcher)?.Shortcut_OnClick(sender, e);
}
