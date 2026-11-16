using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Clock.Locales;
using Clock.Models;
using Clock.Services;
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
    private readonly Guid widgetId;
    private PomodoroPhase phase = PomodoroPhase.Focus;
    private int totalSeconds;
    private int remainingSeconds;
    private int completedSessions;
    private bool isRunning;
    private DateTime? targetEndUtc;

    public PomodoroViewModel(PomodoroModel model) : this(model, Guid.Empty) { }

    public PomodoroViewModel(PomodoroModel model, Guid widgetId)
    {
        this.model = model;
        this.widgetId = widgetId;

        var saved = ClockRuntimeStateStore.GetPomodoro(widgetId);
        if (saved != null)
        {
            phase = saved.Phase;
            completedSessions = saved.CompletedSessions;
            totalSeconds = GetDurationForPhase(phase);
            if (saved.IsRunning && saved.TargetEndUtc.HasValue)
            {
                var remaining = (int)Math.Ceiling((saved.TargetEndUtc.Value - DateTime.UtcNow).TotalSeconds);
                if (remaining > 0)
                {
                    remainingSeconds = Math.Min(totalSeconds, remaining);
                    targetEndUtc = saved.TargetEndUtc.Value;
                    isRunning = true;
                }
                else
                {
                    AdvancePhase(keepRunning: false);
                }
            }
            else
            {
                remainingSeconds = Math.Clamp(saved.PausedRemainingSeconds, 0, totalSeconds);
                if (remainingSeconds == 0) remainingSeconds = totalSeconds;
                isRunning = false;
            }
        }
        else
        {
            SetPhase(PomodoroPhase.Focus);
        }

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
        ? 37.7 * (1.0 - (double)remainingSeconds / totalSeconds)
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
        if (IsRunning)
        {
            if (targetEndUtc.HasValue)
                remainingSeconds = Math.Max(0, (int)Math.Ceiling((targetEndUtc.Value - DateTime.UtcNow).TotalSeconds));
            targetEndUtc = null;
            IsRunning = false;
        }
        else
        {
            if (remainingSeconds <= 0)
                SetPhase(phase);
            targetEndUtc = DateTime.UtcNow.AddSeconds(remainingSeconds);
            IsRunning = true;
        }

        PersistState();
        NotifyTimeUpdated();
    }

    public void Reset()
    {
        targetEndUtc = null;
        IsRunning = false;
        SetPhase(phase);
        PersistState();
    }

    public void SkipPhase()
    {
        targetEndUtc = null;
        IsRunning = false;
        AdvancePhase(keepRunning: false);
        PersistState();
    }

    private void OnTick()
    {
        if (!IsRunning) return;

        if (targetEndUtc.HasValue)
            remainingSeconds = Math.Max(0, (int)Math.Ceiling((targetEndUtc.Value - DateTime.UtcNow).TotalSeconds));
        else if (remainingSeconds > 0)
            remainingSeconds--;

        if (remainingSeconds > 0)
        {
            NotifyTimeUpdated();
        }
        else
        {
            PlayCompletionChime();
            var keepRunning = model.AutoStartNextPhase;
            AdvancePhase(keepRunning);
            PersistState();
        }
    }

    private void AdvancePhase(bool keepRunning)
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

        IsRunning = keepRunning;
        targetEndUtc = keepRunning ? DateTime.UtcNow.AddSeconds(remainingSeconds) : null;
    }

    private int GetDurationForPhase(PomodoroPhase p)
    {
        var minutes = p switch
        {
            PomodoroPhase.Focus => Math.Max(1, model.FocusMinutes),
            PomodoroPhase.ShortBreak => Math.Max(1, model.ShortBreakMinutes),
            PomodoroPhase.LongBreak => Math.Max(1, model.LongBreakMinutes),
            _ => 25
        };
        return minutes * 60;
    }

    private void SetPhase(PomodoroPhase nextPhase)
    {
        phase = nextPhase;
        totalSeconds = GetDurationForPhase(phase);
        remainingSeconds = totalSeconds;
        this.RaisePropertyChanged(nameof(PhaseText));
        NotifyTimeUpdated();
    }

    private void PersistState()
    {
        ClockRuntimeStateStore.SavePomodoro(
            widgetId,
            new PomodoroRuntimeState(phase, totalSeconds, remainingSeconds, targetEndUtc, completedSessions, IsRunning));
    }

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private static void PlayCompletionChime()
    {
        try
        {
            if (OperatingSystem.IsWindows())
                MessageBeep(0x00000040);
        }
        catch
        {
        }
    }

    private void NotifyTimeUpdated()
    {
        this.RaisePropertyChanged(nameof(TimeText));
        this.RaisePropertyChanged(nameof(StrokeDashOffset));
    }

    public void Dispose()
    {
        PersistState();
        TimerService.Timer1Second.Unsubscribe(OnTick);
        GC.SuppressFinalize(this);
    }
}
