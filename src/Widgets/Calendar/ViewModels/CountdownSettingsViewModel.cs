using System;
using System.Text.Json;
using Calendar.Locales;
using Calendar.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Calendar.ViewModels;

public class CountdownSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider? widgetLayoutProvider;
    private CountdownModel model;

    public CountdownSettingsViewModel(IWidgetLayoutProvider? widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<CountdownModel>()
            ?? new CountdownModel(
                Locale.Calendar_Countdown_DefaultEvent,
                new DateTime(DateTime.Now.Year + 1, 1, 1),
                new DateTime(DateTime.Now.Year, 1, 1),
                true);
    }

    public string Title
    {
        get => model.Title;
        set
        {
            if (model.Title != value)
            {
                Save(model with { Title = value });
                this.RaisePropertyChanged(nameof(Title));
            }
        }
    }

    public DateTime? TargetDate
    {
        get => model.TargetDate;
        set
        {
            if (value.HasValue && model.TargetDate != value.Value.Date)
            {
                Save(model with
                {
                    TargetDate = value.Value.Date,
                    StartDate = DateTime.Now.Date
                });
                this.RaisePropertyChanged(nameof(TargetDate));
            }
        }
    }

    public bool ShowHours
    {
        get => model.ShowHours;
        set
        {
            if (model.ShowHours != value)
            {
                Save(model with { ShowHours = value });
                this.RaisePropertyChanged(nameof(ShowHours));
            }
        }
    }

    private void Save(CountdownModel updated)
    {
        model = updated;
        if (widgetLayoutProvider == null) return;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
