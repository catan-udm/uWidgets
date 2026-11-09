using Avalonia.Controls;
using Avalonia.Interactivity;
using Photos.Models;
using Photos.ViewModels;
using Photos.Views.Controls;

namespace Photos.Views;

public partial class PhotoFrame : UserControl
{
    private readonly PhotoFrameViewModel viewModel;

    public PhotoFrame() : this(new PhotoFrameModel()) { }

    public PhotoFrame(PhotoFrameModel model)
    {
        viewModel = new PhotoFrameViewModel(model);
        Content = new PhotoFrameSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Content = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => new PhotoFrameTiny(viewModel),
            { Width: > 230, Height: > 230 } => new PhotoFrameLarge(viewModel),
            { Width: > 230 } => new PhotoFrameWide(viewModel),
            _ => new PhotoFrameSmall(viewModel)
        };
    }

    public void Prev_OnClick(object? sender, RoutedEventArgs e) => viewModel.PreviousImage();

    public void Next_OnClick(object? sender, RoutedEventArgs e) => viewModel.NextImage();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
