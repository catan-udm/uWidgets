using System;
using System.Text.Json;
using Clock.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Clock.ViewModels;

public class PomodoroSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider) : ReactiveObject
{
    private PomodoroModel model = widgetLayoutProvider.Get().GetModel<PomodoroModel>() ?? new PomodoroModel();

    public string TaskTitle
    {
        get => model.TaskTitle;
        set => Save(model with { TaskTitle = value });
    }

    public int FocusMinutes
    {
        get => model.FocusMinutes;
        set => Save(model with { FocusMinutes = Math.Clamp(value, 1, 180) });
    }

    public int ShortBreakMinutes
    {
        get => model.ShortBreakMinutes;
        set => Save(model with { ShortBreakMinutes = Math.Clamp(value, 1, 60) });
    }

    public int LongBreakMinutes
    {
        get => model.LongBreakMinutes;
        set => Save(model with { LongBreakMinutes = Math.Clamp(value, 1, 90) });
    }

    private void Save(PomodoroModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
