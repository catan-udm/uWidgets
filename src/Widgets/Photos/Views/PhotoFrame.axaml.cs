using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Photos.Models;
using Photos.ViewModels;
using Photos.Views.Controls;

namespace Photos.Views;

public partial class PhotoFrame : UserControl
{
    private readonly PhotoFrameViewModel viewModel;
    private string? currentTier;

    public PhotoFrame() : this(new PhotoFrameModel()) { }

    public PhotoFrame(PhotoFrameModel model)
    {
        viewModel = new PhotoFrameViewModel(model);
        currentTier = "Small";
        Content = new PhotoFrameSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var nextTier = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => "Tiny",
            { Width: > 230, Height: > 230 } => "Large",
            { Width: > 230 } => "Wide",
            _ => "Small"
        };

        if (nextTier == currentTier) return;
        currentTier = nextTier;

        Content = nextTier switch
        {
            "Tiny" => new PhotoFrameTiny(viewModel),
            "Large" => new PhotoFrameLarge(viewModel),
            "Wide" => new PhotoFrameWide(viewModel),
            _ => new PhotoFrameSmall(viewModel)
        };
    }

    public void Prev_OnClick(object? sender, RoutedEventArgs e) => viewModel.PreviousImage();

    public void Next_OnClick(object? sender, RoutedEventArgs e) => viewModel.NextImage();

    public void TogglePause_OnClick(object? sender, RoutedEventArgs e) => viewModel.TogglePause();

    public void OpenPhoto_OnDoubleTapped(object? sender, TappedEventArgs e) => viewModel.OpenCurrentImage();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
