using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.Models;
using Shortcuts.ViewModels;

namespace Shortcuts.Views;

public partial class Launcher : UserControl
{
    public Launcher() : this(LauncherModel.CreateDefault()) { }

    public Launcher(LauncherModel model)
    {
        DataContext = new LauncherViewModel(model);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Margin = new(e.NewSize.Height < 110 ? 6 : 10);
    }

    private void Shortcut_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ShortcutItemViewModel item })
        {
            item.Launch();
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }
}
