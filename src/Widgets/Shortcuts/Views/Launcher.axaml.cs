using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.Models;
using Shortcuts.ViewModels;
using Shortcuts.Views.Controls;

namespace Shortcuts.Views;

public partial class Launcher : UserControl
{
    private readonly LauncherViewModel viewModel;

    public Launcher() : this(LauncherModel.CreateDefault()) { }

    public Launcher(LauncherModel model)
    {
        viewModel = new LauncherViewModel(model);
        Content = new LauncherSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Content = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => new LauncherTiny(viewModel),
            { Width: > 230, Height: > 230 } => new LauncherLarge(viewModel),
            { Width: > 230 } => new LauncherWide(viewModel),
            _ => new LauncherSmall(viewModel)
        };
    }

    public void Shortcut_OnClick(object? sender, RoutedEventArgs e)
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
