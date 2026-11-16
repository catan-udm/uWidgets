using System;
using System.Text.Json;
using Notes.Locales;
using Notes.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Notes.ViewModels;

public class NoteSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private NoteModel model = widgetLayoutProvider.Get().GetModel<NoteModel>() ?? new NoteModel(Locale.Notes_Title);

    public double FontSize
    {
        get => Math.Clamp(model.FontSize <= 0 ? 13.0 : model.FontSize, 10.0, 24.0);
        set
        {
            var clamped = Math.Clamp(value, 10.0, 24.0);
            Save(model with { FontSize = clamped });
            this.RaisePropertyChanged();
        }
    }

    public bool ShowTimestamp
    {
        get => model.ShowTimestamp;
        set
        {
            Save(model with { ShowTimestamp = value });
            this.RaisePropertyChanged();
        }
    }

    private void Save(NoteModel updated)
    {
        model = updated;
        var layout = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(layout with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
