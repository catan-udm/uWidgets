using Avalonia.Controls;
using Avalonia.Interactivity;
using Media.Models;
using Media.ViewModels;

namespace Media.Views;

public partial class NowPlaying : UserControl
{
    private readonly NowPlayingViewModel viewModel;

    public NowPlaying() : this(new NowPlayingModel()) { }

    public NowPlaying(NowPlayingModel model)
    {
        viewModel = new NowPlayingViewModel(model);
        DataContext = viewModel;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void Prev_OnClick(object? sender, RoutedEventArgs e) => viewModel.PreviousTrack();

    private void PlayPause_OnClick(object? sender, RoutedEventArgs e) => viewModel.PlayPause();

    private void Next_OnClick(object? sender, RoutedEventArgs e) => viewModel.NextTrack();

    private void Mute_OnClick(object? sender, RoutedEventArgs e) => viewModel.ToggleMute();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
