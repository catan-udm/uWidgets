using System.Text.Json;
using Finance.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Finance.ViewModels;

public class ConverterSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private ConverterModel model;

    public ConverterSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<ConverterModel>() ?? new ConverterModel();
    }

    public double RootFontSizePx
    {
        get => model.RootFontSizePx;
        set
        {
            var clamped = Math.Clamp(value, 8.0, 64.0);
            if (Math.Abs(model.RootFontSizePx - clamped) < 0.001) return;
            model = model with { RootFontSizePx = clamped };
            SaveModel();
            this.RaisePropertyChanged(nameof(RootFontSizePx));
        }
    }

    private void SaveModel()
    {
        if (widgetLayoutProvider == null) return;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
