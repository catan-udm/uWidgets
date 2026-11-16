using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.ViewModels;

namespace Shortcuts.Views.Controls;

public partial class LauncherLarge : UserControl
{
    public LauncherLarge()
    {
        InitializeComponent();
    }

    public LauncherLarge(LauncherViewModel viewModel)
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
