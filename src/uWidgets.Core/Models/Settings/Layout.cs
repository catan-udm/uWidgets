namespace uWidgets.Core.Models.Settings;

/// <summary>
/// Controls how desktop widgets are layered relative to other windows.
/// </summary>
public enum LayeringMode
{
    /// <summary>
    /// Pinned directly onto the Windows desktop layer (WorkerW/Progman).
    /// </summary>
    DesktopPinned = 0,

    /// <summary>
    /// Normal floating desktop windows.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Kept above regular windows.
    /// </summary>
    AlwaysOnTop = 2
}

/// <summary>
/// Widget sizing and positioning settings.
/// </summary>
/// <param name="SnapSize">Should widget's size be snapped to the grid</param>
/// <param name="LockSize">Disable resizing a widget</param>
/// <param name="SnapPosition">Should widget's position be snapped to the grid</param>
/// <param name="LockPosition">Disable moving a widget</param>
/// <param name="LayeringMode">Window Z-order layering behavior</param>
public record Layout(
    bool SnapSize,
    bool LockSize,
    bool SnapPosition,
    bool LockPosition,
    LayeringMode LayeringMode = LayeringMode.DesktopPinned);