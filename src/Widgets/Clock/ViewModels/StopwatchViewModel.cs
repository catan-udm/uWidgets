using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Clock.Locales;
using Clock.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Clock.ViewModels;

public record LapItemViewModel(string Label, string ElapsedText);

public class StopwatchViewModel : ReactiveObject, IDisposable
{
    private static readonly StreamGeometry PlayGeometry = StreamGeometry.Parse("m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z");
    private static readonly StreamGeometry PauseGeometry = StreamGeometry.Parse("M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5zm5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5z");

    private readonly Guid widgetId;
    private TimeSpan accumulated = TimeSpan.Zero;
    private DateTime? startedUtc;
    private bool isRunning;

    public StopwatchViewModel() : this(Guid.Empty) { }

    public StopwatchViewModel(Guid widgetId)
    {
        this.widgetId = widgetId;

        var saved = ClockRuntimeStateStore.GetStopwatch(widgetId);
        if (saved != null)
        {
            accumulated = TimeSpan.FromTicks(Math.Max(0, saved.AccumulatedTicks));
            startedUtc = saved.StartedUtc;
            isRunning = saved.IsRunning && startedUtc.HasValue;
            if (saved.Laps != null)
            {
                foreach (var lap in saved.Laps)
                    Laps.Add(lap);
            }
        }

        TimerService.Timer100Ms.Subscribe(OnTick);
    }

    public ObservableCollection<LapItemViewModel> Laps { get; } = [];

    private TimeSpan CurrentElapsed =>
        isRunning && startedUtc.HasValue
            ? accumulated + (DateTime.UtcNow - startedUtc.Value).Duration()
            : accumulated;

    public string MainTimeText
    {
        get
        {
            var elapsed = CurrentElapsed;
            return elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }
    }

    public string DecisecondsText => $".{CurrentElapsed.Milliseconds / 100}";

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            this.RaiseAndSetIfChanged(ref isRunning, value);
            this.RaisePropertyChanged(nameof(PlayPauseIcon));
        }
    }

    public bool HasLaps => Laps.Count > 0;
    public string CompactLapSummary => Laps.Count > 0 ? $"L{Laps.Count}: {Laps[0].ElapsedText}" : string.Empty;

    public StreamGeometry PlayPauseIcon => IsRunning ? PauseGeometry : PlayGeometry;

    public void ToggleStartPause()
    {
        if (IsRunning)
        {
            accumulated = CurrentElapsed;
            startedUtc = null;
            IsRunning = false;
        }
        else
        {
            startedUtc = DateTime.UtcNow;
            IsRunning = true;
        }

        PersistState();
        NotifyTimeChanged();
    }

    public void RecordLap()
    {
        if (!IsRunning && CurrentElapsed == TimeSpan.Zero) return;
        var index = Laps.Count + 1;
        Laps.Insert(0, new LapItemViewModel($"{Locale.Clock_Stopwatch_Lap} {index}", $"{MainTimeText}{DecisecondsText}"));
        this.RaisePropertyChanged(nameof(HasLaps));
        this.RaisePropertyChanged(nameof(CompactLapSummary));
        PersistState();
    }

    public void Reset()
    {
        accumulated = TimeSpan.Zero;
        startedUtc = null;
        IsRunning = false;
        Laps.Clear();
        this.RaisePropertyChanged(nameof(HasLaps));
        this.RaisePropertyChanged(nameof(CompactLapSummary));
        PersistState();
        NotifyTimeChanged();
    }

    private void PersistState()
    {
        ClockRuntimeStateStore.SaveStopwatch(
            widgetId,
            new StopwatchRuntimeState(accumulated.Ticks, startedUtc, IsRunning, Laps.ToList()));
    }

    private void OnTick()
    {
        if (!IsRunning) return;
        NotifyTimeChanged();
    }

    private void NotifyTimeChanged()
    {
        this.RaisePropertyChanged(nameof(MainTimeText));
        this.RaisePropertyChanged(nameof(DecisecondsText));
    }

    public void Dispose()
    {
        PersistState();
        TimerService.Timer100Ms.Unsubscribe(OnTick);
        GC.SuppressFinalize(this);
    }
}
