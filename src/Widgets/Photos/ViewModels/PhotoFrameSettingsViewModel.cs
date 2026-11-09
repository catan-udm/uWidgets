using System;
using System.Text.Json;
using Photos.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Photos.ViewModels;

public class PhotoFrameSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private PhotoFrameModel model = widgetLayoutProvider.Get().GetModel<PhotoFrameModel>() ?? new PhotoFrameModel();

    public string Path
    {
        get => model.Path;
        set
        {
            Save(model with { Path = value });
            this.RaisePropertyChanged();
        }
    }

    public int IntervalMinutes
    {
        get => model.IntervalMinutes;
        set => Save(model with { IntervalMinutes = Math.Clamp(value, 1, 1440) });
    }

    public bool FillWidget
    {
        get => model.FillWidget;
        set => Save(model with { FillWidget = value });
    }

    public string Caption
    {
        get => model.Caption;
        set => Save(model with { Caption = value });
    }

    private void Save(PhotoFrameModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
