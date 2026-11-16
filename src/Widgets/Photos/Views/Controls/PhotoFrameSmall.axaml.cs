using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Photos.ViewModels;

namespace Photos.Views.Controls;

public partial class PhotoFrameSmall : UserControl
{
    public PhotoFrameSmall()
    {
        InitializeComponent();
    }

    public PhotoFrameSmall(PhotoFrameViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Prev_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.Prev_OnClick(sender, e);

    private void TogglePause_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.TogglePause_OnClick(sender, e);

    private void Next_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.Next_OnClick(sender, e);

    private void OpenPhoto_OnDoubleTapped(object? sender, TappedEventArgs e) =>
        (Parent as PhotoFrame)?.OpenPhoto_OnDoubleTapped(sender, e);
}
