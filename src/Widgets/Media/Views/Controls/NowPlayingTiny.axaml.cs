using Avalonia.Controls;
using Avalonia.Interactivity;
using Media.ViewModels;

namespace Media.Views.Controls;

public partial class NowPlayingTiny : UserControl
{
    public NowPlayingTiny()
    {
        InitializeComponent();
    }

    public NowPlayingTiny(NowPlayingViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void PlayPause_OnClick(object? sender, RoutedEventArgs e) =>
        (Parent as NowPlaying)?.PlayPause_OnClick(sender, e);
}
