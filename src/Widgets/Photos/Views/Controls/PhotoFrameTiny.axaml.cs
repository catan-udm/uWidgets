using Avalonia.Controls;
using Avalonia.Interactivity;
using Photos.ViewModels;

namespace Photos.Views.Controls;

public partial class PhotoFrameTiny : UserControl
{
    public PhotoFrameTiny()
    {
        InitializeComponent();
    }

    public PhotoFrameTiny(PhotoFrameViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Next_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as PhotoFrame)?.Next_OnClick(sender, e);
}
