using System.Text.Json;
using Development.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Development.ViewModels;

public class DockerSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private DockerModel model;

    public DockerSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<DockerModel>() ?? new DockerModel();
    }

    public bool ShowAllContainers
    {
        get => model.ShowAllContainers;
        set
        {
            model = model with { ShowAllContainers = value };
            SaveModel();
            this.RaisePropertyChanged(nameof(ShowAllContainers));
        }
    }

    private void SaveModel()
    {
        if (widgetLayoutProvider == null) return;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
