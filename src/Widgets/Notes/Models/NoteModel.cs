namespace Notes.Models;

public record NoteModel(
    string? Title = null, 
    string? Content = null, 
    DateTime? Updated = null,
    double FontSize = 13.0,
    bool ShowTimestamp = true);