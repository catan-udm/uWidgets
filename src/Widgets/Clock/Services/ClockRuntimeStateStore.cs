using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Clock.ViewModels;
using uWidgets.Core;

namespace Clock.Services;

public record PomodoroRuntimeState(
    PomodoroPhase Phase,
    int TotalSeconds,
    int PausedRemainingSeconds,
    DateTime? TargetEndUtc,
    int CompletedSessions,
    bool IsRunning);

public record StopwatchRuntimeState(
    long AccumulatedTicks,
    DateTime? StartedUtc,
    bool IsRunning,
    List<LapItemViewModel> Laps);

public sealed class ClockRuntimeContainer
{
    public Dictionary<Guid, PomodoroRuntimeState> Pomodoros { get; set; } = new();
    public Dictionary<Guid, StopwatchRuntimeState> Stopwatches { get; set; } = new();
}

public static class ClockRuntimeStateStore
{
    private static readonly object Sync = new();
    private static readonly string StatePath = Path.Combine(Const.CurrentFolder, "clock_timers.json");
    private static ClockRuntimeContainer? cache;

    public static PomodoroRuntimeState? GetPomodoro(Guid id)
    {
        if (id == Guid.Empty) return null;
        lock (Sync)
        {
            return EnsureLoaded().Pomodoros.TryGetValue(id, out var state) ? state : null;
        }
    }

    public static void SavePomodoro(Guid id, PomodoroRuntimeState state)
    {
        if (id == Guid.Empty) return;
        lock (Sync)
        {
            var container = EnsureLoaded();
            container.Pomodoros[id] = state;
            SaveInternal(container);
        }
    }

    public static StopwatchRuntimeState? GetStopwatch(Guid id)
    {
        if (id == Guid.Empty) return null;
        lock (Sync)
        {
            return EnsureLoaded().Stopwatches.TryGetValue(id, out var state) ? state : null;
        }
    }

    public static void SaveStopwatch(Guid id, StopwatchRuntimeState state)
    {
        if (id == Guid.Empty) return;
        lock (Sync)
        {
            var container = EnsureLoaded();
            container.Stopwatches[id] = state;
            SaveInternal(container);
        }
    }

    private static ClockRuntimeContainer EnsureLoaded()
    {
        if (cache != null) return cache;
        try
        {
            if (File.Exists(StatePath))
            {
                var json = File.ReadAllText(StatePath);
                cache = JsonSerializer.Deserialize<ClockRuntimeContainer>(json) ?? new ClockRuntimeContainer();
                return cache;
            }
        }
        catch
        {
        }

        cache = new ClockRuntimeContainer();
        return cache;
    }

    private static void SaveInternal(ClockRuntimeContainer container)
    {
        try
        {
            Directory.CreateDirectory(Const.CurrentFolder);
            var json = JsonSerializer.Serialize(container);
            var tmp = $"{StatePath}.tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, StatePath, overwrite: true);
        }
        catch
        {
        }
    }
}
