using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Calendar.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Calendar.ViewModels;

public record DayOfWeekOption(DayOfWeek Day, string DisplayName);

public class MonthCalendarSettingsViewModel(IWidgetLayoutProvider? widgetLayoutProvider) : ReactiveObject
{
    public MonthCalendarModel Model =>
        widgetLayoutProvider?.Get()?.GetModel<MonthCalendarModel>() 
        ?? new MonthCalendarModel(DayOfWeek.Monday);

    public DayOfWeekOption[] Days { get; } = Enum.GetValues<DayOfWeek>()
        .Select(d => new DayOfWeekOption(d, Thread.CurrentThread.CurrentUICulture.DateTimeFormat.GetDayName(d)))
        .ToArray();

    public DayOfWeekOption SelectedDay
    {
        get => Days.FirstOrDefault(d => d.Day == Model.FirstDayOfWeek) ?? Days[0];
        set
        {
            if (value == null || value.Day == Model.FirstDayOfWeek) return;
            FirstDayOfWeek = value.Day;
            this.RaisePropertyChanged(nameof(SelectedDay));
        }
    }
    
    public DayOfWeek FirstDayOfWeek
    {
        get => Model.FirstDayOfWeek;
        set
        {
            if (widgetLayoutProvider == null) return;
            var newModel = Model with { FirstDayOfWeek = value };
            var newSettings = widgetLayoutProvider.Get() with
            {
                Settings = JsonSerializer.SerializeToElement(newModel)
            };
            widgetLayoutProvider.Save(newSettings);
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(SelectedDay));
        }
    }
}
