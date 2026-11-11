using System.Text.Json;
using Monitor.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Monitor.ViewModels;

public class DeviceBatteriesSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private DeviceBatteriesModel model;

    public DeviceBatteriesSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<DeviceBatteriesModel>() ?? new DeviceBatteriesModel();
    }

    public bool ShowPercentages
    {
        get => model.ShowPercentages;
        set
        {
            model = model with { ShowPercentages = value };
            SaveModel();
            this.RaisePropertyChanged(nameof(ShowPercentages));
        }
    }

    public bool OnlyConnected
    {
        get => model.OnlyConnected;
        set
        {
            model = model with { OnlyConnected = value };
            SaveModel();
            this.RaisePropertyChanged(nameof(OnlyConnected));
        }
    }

    private void SaveModel()
    {
        if (widgetLayoutProvider == null) return;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
