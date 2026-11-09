using System;
using System.Text.Json;
using Calendar.Locales;
using Calendar.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Calendar.ViewModels;

public class CountdownSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private CountdownModel model = widgetLayoutProvider.Get().GetModel<CountdownModel>()
        ?? new CountdownModel(
            Locale.Calendar_Countdown_DefaultEvent,
            new DateTime(DateTime.Now.Year + 1, 1, 1),
            new DateTime(DateTime.Now.Year, 1, 1),
            true);

    public string Title
    {
        get => model.Title;
        set => Save(model with { Title = value });
    }

    public DateTimeOffset? TargetDate
    {
        get => new DateTimeOffset(model.TargetDate);
        set
        {
            if (value.HasValue)
            {
                Save(model with
                {
                    TargetDate = value.Value.DateTime.Date,
                    StartDate = DateTime.Now.Date
                });
            }
        }
    }

    public bool ShowHours
    {
        get => model.ShowHours;
        set => Save(model with { ShowHours = value });
    }

    private void Save(CountdownModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
