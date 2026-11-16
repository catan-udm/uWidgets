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
        Launcher.HandleShortcutClick(sender);

    private void CopyTarget_OnClick(object? sender, RoutedEventArgs e) =>
        Launcher.HandleCopyTargetClick(this, sender);

    private void OpenContainingFolder_OnClick(object? sender, RoutedEventArgs e) =>
        Launcher.HandleOpenFolderClick(sender);
}
