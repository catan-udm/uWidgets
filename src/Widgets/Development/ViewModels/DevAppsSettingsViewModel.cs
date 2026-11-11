using System.Text.Json;
using Development.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Development.ViewModels;

public class DevAppsSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private DevAppsModel model;

    public DevAppsSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<DevAppsModel>() ?? new DevAppsModel();
    }

    public int RefreshIntervalSeconds
    {
        get => model.RefreshIntervalSeconds;
        set
        {
            model = model with { RefreshIntervalSeconds = value };
            SaveModel();
            this.RaisePropertyChanged(nameof(RefreshIntervalSeconds));
        }
    }

    private void SaveModel()
    {
        if (widgetLayoutProvider == null) return;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
