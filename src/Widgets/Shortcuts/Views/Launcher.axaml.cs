using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Shortcuts.Models;
using Shortcuts.ViewModels;
using Shortcuts.Views.Controls;

namespace Shortcuts.Views;

public partial class Launcher : UserControl
{
    private readonly LauncherViewModel viewModel;
    private string currentTier;

    public Launcher() : this(LauncherModel.CreateDefault()) { }

    public Launcher(LauncherModel model)
    {
        viewModel = new LauncherViewModel(model);
        DataContext = viewModel;
        InitializeComponent();
        currentTier = "small";
        TierHost.Content = new LauncherSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var tier = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => "tiny",
            { Width: > 230, Height: > 230 } => "large",
            { Width: > 230 } => "wide",
            _ => "small"
        };

        if (tier == currentTier) return;
        currentTier = tier;

        TierHost.Content = tier switch
        {
            "tiny" => new LauncherTiny(viewModel),
            "large" => new LauncherLarge(viewModel),
            "wide" => new LauncherWide(viewModel),
            _ => new LauncherSmall(viewModel)
        };
    }

    public static void HandleShortcutClick(object? sender)
    {
        if (sender is StyledElement { DataContext: ShortcutItemViewModel item })
        {
            item.Launch();
        }
    }

    public static async void HandleCopyTargetClick(Control owner, object? sender)
    {
        if (sender is StyledElement { DataContext: ShortcutItemViewModel item })
        {
            await item.CopyTargetAsync(TopLevel.GetTopLevel(owner));
        }
    }

    public static void HandleOpenFolderClick(object? sender)
    {
        if (sender is StyledElement { DataContext: ShortcutItemViewModel item })
        {
            item.OpenContainingFolder();
        }
    }

    private void DismissStatus_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.StatusMessage = null;

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }
}
