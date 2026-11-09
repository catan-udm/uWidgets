using Avalonia.Controls;
using Avalonia.Interactivity;
using Media.Models;
using Media.ViewModels;
using Media.Views.Controls;

namespace Media.Views;

public partial class NowPlaying : UserControl
{
    private readonly NowPlayingViewModel viewModel;

    public NowPlaying() : this(new NowPlayingModel()) { }

    public NowPlaying(NowPlayingModel model)
    {
        viewModel = new NowPlayingViewModel(model);
        Content = new NowPlayingSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Content = e.NewSize switch
        {
            { Width: <= 120 } or { Height: <= 120 } => new NowPlayingTiny(viewModel),
            { Width: > 230, Height: > 230 } => new NowPlayingLarge(viewModel),
            { Width: > 230 } => new NowPlayingWide(viewModel),
            _ => new NowPlayingSmall(viewModel)
        };
    }

    public void Prev_OnClick(object? sender, RoutedEventArgs e) => viewModel.PreviousTrack();

    public void PlayPause_OnClick(object? sender, RoutedEventArgs e) => viewModel.PlayPause();

    public void Next_OnClick(object? sender, RoutedEventArgs e) => viewModel.NextTrack();

    public void Mute_OnClick(object? sender, RoutedEventArgs e) => viewModel.ToggleMute();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
