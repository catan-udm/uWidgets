namespace Photos.Models;

public record PhotoFrameModel(
    string Path = "",
    int IntervalMinutes = 5,
    bool FillWidget = true,
    string Caption = "",
    bool Shuffle = false,
    bool IncludeSubfolders = false
);
