namespace Clock.Models;

public record PomodoroModel(
    int FocusMinutes = 25,
    int ShortBreakMinutes = 5,
    int LongBreakMinutes = 15,
    string TaskTitle = ""
);
