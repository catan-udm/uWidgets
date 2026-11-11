using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media;
using Clock.Locales;
using ReactiveUI;
using uWidgets.Services;

namespace Clock.ViewModels;

public record LapItemViewModel(string Label, string ElapsedText);

public class StopwatchViewModel : ReactiveObject, IDisposable
{
    private static readonly StreamGeometry PlayGeometry = StreamGeometry.Parse("m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z");
    private static readonly StreamGeometry PauseGeometry = StreamGeometry.Parse("M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5zm5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5z");

    private readonly Stopwatch stopwatch = new();
    private bool isRunning;

    public StopwatchViewModel()
    {
        TimerService.Timer100Ms.Subscribe(OnTick);
    }

    public ObservableCollection<LapItemViewModel> Laps { get; } = [];

    public string MainTimeText
    {
        get
        {
            var elapsed = stopwatch.Elapsed;
            return elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }
    }

    public string DecisecondsText => $".{stopwatch.Elapsed.Milliseconds / 100}";

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

    public StreamGeometry PlayPauseIcon => IsRunning ? PauseGeometry : PlayGeometry;

    public void ToggleStartPause()
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
            IsRunning = false;
        }
        else
        {
            stopwatch.Start();
            IsRunning = true;
        }
        NotifyTimeChanged();
    }

    public void RecordLap()
    {
        if (!stopwatch.IsRunning && stopwatch.Elapsed == TimeSpan.Zero) return;
        var index = Laps.Count + 1;
        Laps.Insert(0, new LapItemViewModel($"{Locale.Clock_Stopwatch_Lap} {index}", $"{MainTimeText}{DecisecondsText}"));
        this.RaisePropertyChanged(nameof(HasLaps));
    }

    public void Reset()
    {
        stopwatch.Reset();
        IsRunning = false;
        Laps.Clear();
        this.RaisePropertyChanged(nameof(HasLaps));
        NotifyTimeChanged();
    }

    private void OnTick()
    {
        if (!stopwatch.IsRunning) return;
        NotifyTimeChanged();
    }

    private void NotifyTimeChanged()
    {
        this.RaisePropertyChanged(nameof(MainTimeText));
        this.RaisePropertyChanged(nameof(DecisecondsText));
    }

    public void Dispose()
    {
        TimerService.Timer100Ms.Unsubscribe(OnTick);
        GC.SuppressFinalize(this);
    }
}
