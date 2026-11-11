using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Media.ViewModels;

namespace Media.Views.Controls;

public partial class NowPlayingTiny : UserControl
{
    private readonly NowPlayingViewModel? viewModel;

    public NowPlayingTiny()
    {
        InitializeComponent();
        AttachTimelineEvents();
    }

    public NowPlayingTiny(NowPlayingViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        AttachTimelineEvents();
    }

    private void AttachTimelineEvents()
    {
        var slider = this.FindControl<Slider>("TimelineSlider");
        if (slider == null) return;

        slider.AddHandler(InputElement.PointerPressedEvent, (s, e) =>
        {
            (viewModel ?? DataContext as NowPlayingViewModel)?.OnSeekStarted();
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        slider.AddHandler(InputElement.PointerReleasedEvent, (s, e) =>
        {
            var vm = viewModel ?? DataContext as NowPlayingViewModel;
            vm?.OnSeekCompleted(slider.Value);
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        slider.AddHandler(InputElement.PointerCaptureLostEvent, (s, e) =>
        {
            var vm = viewModel ?? DataContext as NowPlayingViewModel;
            vm?.OnSeekCompleted(slider.Value);
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void PlayPause_OnClick(object? sender, RoutedEventArgs e) =>
        (viewModel ?? DataContext as NowPlayingViewModel)?.PlayPause();
}
