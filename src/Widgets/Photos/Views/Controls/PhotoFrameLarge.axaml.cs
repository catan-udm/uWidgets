using Avalonia.Controls;
using Avalonia.Interactivity;
using Photos.ViewModels;

namespace Photos.Views.Controls;

public partial class PhotoFrameLarge : UserControl
{
    public PhotoFrameLarge()
    {
        InitializeComponent();
    }

    public PhotoFrameLarge(PhotoFrameViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Prev_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.Prev_OnClick(sender, e);

    private void Next_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.Next_OnClick(sender, e);
}
