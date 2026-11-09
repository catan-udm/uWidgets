using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Photos.Models;
using Photos.ViewModels;

namespace Photos.Views;

public partial class PhotoFrame : UserControl
{
    private readonly PhotoFrameViewModel viewModel;
    private readonly PhotoFrameModel model;

    public PhotoFrame() : this(new PhotoFrameModel()) { }

    public PhotoFrame(PhotoFrameModel model)
    {
        this.model = model;
        viewModel = new PhotoFrameViewModel(model);
        DataContext = viewModel;
        Unloaded += OnUnloaded;
        InitializeComponent();
        Margin = model.FillWidget ? new Thickness(0) : new Thickness(10);
        FrameBorder.CornerRadius = model.FillWidget ? new CornerRadius(0) : new CornerRadius(12);
    }

    private void Next_OnClick(object? sender, RoutedEventArgs e) => viewModel.NextImage();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
