using Avalonia.Controls;
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

    private void Next_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.Next_OnClick(sender, e);
}
