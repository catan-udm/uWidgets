using System;
using Avalonia.Media;
using Clock.Locales;
using Clock.Models;
using ReactiveUI;
using uWidgets.Services;

namespace Clock.ViewModels;

public enum PomodoroPhase
{
    Focus,
    ShortBreak,
    LongBreak
}

public class PomodoroViewModel : ReactiveObject, IDisposable
{
    private static readonly StreamGeometry PlayGeometry = StreamGeometry.Parse("m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z");
    private static readonly StreamGeometry PauseGeometry = StreamGeometry.Parse("M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5zm5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5z");

    private readonly PomodoroModel model;
    private PomodoroPhase phase = PomodoroPhase.Focus;
    private int totalSeconds;
    private int remainingSeconds;
    private int completedSessions;
    private bool isRunning;

    public PomodoroViewModel(PomodoroModel model)
    {
        this.model = model;
        SetPhase(PomodoroPhase.Focus);
        TimerService.Timer1Second.Subscribe(OnTick);
    }

    public string TaskTitle => string.IsNullOrWhiteSpace(model.TaskTitle)
        ? Locale.Clock_Pomodoro_Title
        : model.TaskTitle;

    public string PhaseText => phase switch
    {
        PomodoroPhase.Focus => Locale.Clock_Pomodoro_PhaseFocus,
        PomodoroPhase.ShortBreak => Locale.Clock_Pomodoro_PhaseShortBreak,
        PomodoroPhase.LongBreak => Locale.Clock_Pomodoro_PhaseLongBreak,
        _ => Locale.Clock_Pomodoro_PhaseFocus
    };

    public string TimeText
    {
        get
        {
            var minutes = remainingSeconds / 60;
            var seconds = remainingSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    public double StrokeDashOffset => totalSeconds > 0
        ? 31.4 * (1.0 - (double)remainingSeconds / totalSeconds)
        : 0;

    public string SessionCounterText => $"#{(completedSessions % 4) + 1}/4";

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            this.RaiseAndSetIfChanged(ref isRunning, value);
            this.RaisePropertyChanged(nameof(PlayPauseIcon));
        }
    }

    public StreamGeometry PlayPauseIcon => IsRunning ? PauseGeometry : PlayGeometry;

    public void ToggleStartPause()
    {
        IsRunning = !IsRunning;
    }

    public void Reset()
    {
        IsRunning = false;
        SetPhase(phase);
    }

    public void SkipPhase()
    {
        IsRunning = false;
        AdvancePhase();
    }

    private void OnTick()
    {
        if (!IsRunning) return;

        if (remainingSeconds > 0)
        {
            remainingSeconds--;
            NotifyTimeUpdated();
        }
        else
        {
            AdvancePhase();
        }
    }

    private void AdvancePhase()
    {
        if (phase == PomodoroPhase.Focus)
        {
            completedSessions++;
            this.RaisePropertyChanged(nameof(SessionCounterText));
            SetPhase(completedSessions % 4 == 0 ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak);
        }
        else
        {
            SetPhase(PomodoroPhase.Focus);
        }
    }

    private void SetPhase(PomodoroPhase nextPhase)
    {
        phase = nextPhase;
        var minutes = phase switch
        {
            PomodoroPhase.Focus => Math.Max(1, model.FocusMinutes),
            PomodoroPhase.ShortBreak => Math.Max(1, model.ShortBreakMinutes),
            PomodoroPhase.LongBreak => Math.Max(1, model.LongBreakMinutes),
            _ => 25
        };
        totalSeconds = minutes * 60;
        remainingSeconds = totalSeconds;
        this.RaisePropertyChanged(nameof(PhaseText));
        NotifyTimeUpdated();
    }

    private void NotifyTimeUpdated()
    {
        this.RaisePropertyChanged(nameof(TimeText));
        this.RaisePropertyChanged(nameof(StrokeDashOffset));
    }

    public void Dispose()
    {
        TimerService.Timer1Second.Unsubscribe(OnTick);
        GC.SuppressFinalize(this);
    }
}
