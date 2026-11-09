using System.Text.Json;
using Media.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Media.ViewModels;

public class NowPlayingSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private NowPlayingModel model = widgetLayoutProvider.Get().GetModel<NowPlayingModel>() ?? new NowPlayingModel();

    public bool ShowVisualizer
    {
        get => model.ShowVisualizer;
        set => Save(model with { ShowVisualizer = value });
    }

    public bool ShowMuteButton
    {
        get => model.ShowMuteButton;
        set => Save(model with { ShowMuteButton = value });
    }

    private void Save(NowPlayingModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
