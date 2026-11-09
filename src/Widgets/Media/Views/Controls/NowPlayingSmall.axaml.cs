using Avalonia.Controls;
using Avalonia.Interactivity;
using Media.ViewModels;

namespace Media.Views.Controls;

public partial class NowPlayingSmall : UserControl
{
    public NowPlayingSmall()
    {
        InitializeComponent();
    }

    public NowPlayingSmall(NowPlayingViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Prev_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as NowPlaying)?.Prev_OnClick(sender, e);

    private void PlayPause_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as NowPlaying)?.PlayPause_OnClick(sender, e);

    private void Next_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as NowPlaying)?.Next_OnClick(sender, e);

    private void Mute_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as NowPlaying)?.Mute_OnClick(sender, e);
}
