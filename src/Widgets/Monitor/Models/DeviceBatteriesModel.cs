namespace Monitor.Models;

public record DeviceBatteriesModel(
    bool ShowPercentages = true,
    bool OnlyConnected = true,
    bool CompactView = false);
