namespace Calendar.Models;

public record CountdownModel(
    string Title,
    DateTime TargetDate,
    DateTime StartDate,
    bool ShowHours = true
);
