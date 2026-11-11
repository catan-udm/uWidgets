using System.Text.Json;
using Media.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Media.ViewModels;

public class NowPlayingSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private NowPlayingModel model = widgetLayoutProvider.Get().GetModel<NowPlayingModel>() ?? new NowPlayingModel();

    public bool ShowTimeline
    {
        get => model.ShowTimeline;
        set => Save(model with { ShowTimeline = value });
    }

    public bool ShowVisualizer
    {
        get => ShowTimeline;
        set => ShowTimeline = value;
    }

    public bool ShowMuteButton
    {
        get => model.ShowMuteButton;
        set => Save(model with { ShowMuteButton = value });
    }

    public bool PrefersMusicPlayer
    {
        get => model.PrefersMusicPlayer;
        set => Save(model with { PrefersMusicPlayer = value });
    }

    public bool AllowBrowserMedia
    {
        get => model.AllowBrowserMedia;
        set => Save(model with { AllowBrowserMedia = value });
    }

    private void Save(NowPlayingModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
