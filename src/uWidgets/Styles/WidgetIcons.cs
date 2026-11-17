using System;
using Avalonia.Media;

namespace uWidgets.Styles;

/// <summary>
/// Shared vector geometries and SVG path constants reused across shell chrome and widget modules.
/// Paths are authored on non-overlapping 16x16 coordinate grids with exact bounds so Avalonia's
/// uniform Viewbox scaling avoids EvenOdd self-intersection dropouts and fractional alignment drift.
/// </summary>
public static class WidgetIcons
{
    public const string PlayPath =
        "M 3,3 A 1,1 0 0 1 4.53,2.152 L 12.53,7.152 A 1,1 0 0 1 12.53,8.848 L 4.53,13.848 A 1,1 0 0 1 3,13 Z";

    public const string PausePath =
        "M4,2 H5 A1,1 0 0 1 6,3 V13 A1,1 0 0 1 5,14 H4 A1,1 0 0 1 3,13 V3 A1,1 0 0 1 4,2 Z M11,2 H12 A1,1 0 0 1 13,3 V13 A1,1 0 0 1 12,14 H11 A1,1 0 0 1 10,13 V3 A1,1 0 0 1 11,2 Z";

    public const string MusicNotePath =
        "M 4.7,3.3 A 0.9,0.9 0 0 1 5.5,2.4 L 14.0,1.01 A 0.9,0.9 0 0 1 15.0,1.9 L 15.0,11.0 A 2.5,2.0 0 1 1 13.7,9.246 L 13.7,2.95 L 6.0,4.0 L 6.0,13.0 A 2.5,2.0 0 1 1 4.7,11.246 Z";

    public const string RefreshPath =
        "M8 3a5 5 0 1 0 4.546 2.914.5.5 0 0 1 .908-.417A6 6 0 1 1 8 2z M8 4.466V.534a.25.25 0 0 1 .41-.192l2.36 1.966c.12.1.12.284 0 .384L8.41 4.658A.25.25 0 0 1 8 4.466";

    public const string ClosePath =
        "M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8z";

    public const string MenuPath =
        "M2 3.5a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5m0 4.5a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5m0 4.5a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5";

    public const string SwapHorizontalPath =
        "M1 11.5a.5.5 0 0 0 .5.5h11.793l-3.147 3.146a.5.5 0 0 0 .708.708l4-4a.5.5 0 0 0 0-.708l-4-4a.5.5 0 0 0-.708.708L13.293 11H1.5a.5.5 0 0 0-.5.5m14-7a.5.5 0 0 1-.5.5H2.707l3.147 3.146a.5.5 0 1 1-.708.708l-4-4a.5.5 0 0 1 0-.708l4-4a.5.5 0 1 1 .708.708L2.707 4H14.5a.5.5 0 0 1 .5.5";

    public const string ArrowRightPath =
        "M1 8a.5.5 0 0 1 .5-.5h11.793l-3.147-3.146a.5.5 0 0 1 .708-.708l4 4a.5.5 0 0 1 0 .708l-4 4a.5.5 0 0 1-.708-.708L13.293 8.5H1.5A.5.5 0 0 1 1 8";

    private static readonly Lazy<StreamGeometry> PlayLazy = new(() => StreamGeometry.Parse(PlayPath));
    private static readonly Lazy<StreamGeometry> PauseLazy = new(() => StreamGeometry.Parse(PausePath));
    private static readonly Lazy<StreamGeometry> MusicNoteLazy = new(() => StreamGeometry.Parse(MusicNotePath));
    private static readonly Lazy<StreamGeometry> RefreshLazy = new(() => StreamGeometry.Parse(RefreshPath));
    private static readonly Lazy<StreamGeometry> CloseLazy = new(() => StreamGeometry.Parse(ClosePath));
    private static readonly Lazy<StreamGeometry> MenuLazy = new(() => StreamGeometry.Parse(MenuPath));
    private static readonly Lazy<StreamGeometry> SwapHorizontalLazy = new(() => StreamGeometry.Parse(SwapHorizontalPath));
    private static readonly Lazy<StreamGeometry> ArrowRightLazy = new(() => StreamGeometry.Parse(ArrowRightPath));

    /// <summary>Gets the shared rounded play triangle geometry (10x12 exact bounds).</summary>
    public static StreamGeometry Play => PlayLazy.Value;

    /// <summary>Gets the shared two-bar pause geometry (10x12 exact bounds).</summary>
    public static StreamGeometry Pause => PauseLazy.Value;

    /// <summary>Gets the shared single-contour beamed eighth-note geometry (14x14 bounds).</summary>
    public static StreamGeometry MusicNote => MusicNoteLazy.Value;

    /// <summary>Gets the shared clockwise refresh arrow geometry.</summary>
    public static StreamGeometry Refresh => RefreshLazy.Value;

    /// <summary>Gets the shared close/dismiss cross geometry.</summary>
    public static StreamGeometry Close => CloseLazy.Value;

    /// <summary>Gets the shared hamburger menu icon geometry.</summary>
    public static StreamGeometry Menu => MenuLazy.Value;

    /// <summary>Gets the shared bidirectional horizontal swap icon geometry.</summary>
    public static StreamGeometry SwapHorizontal => SwapHorizontalLazy.Value;

    /// <summary>Gets the shared right-pointing arrow geometry.</summary>
    public static StreamGeometry ArrowRight => ArrowRightLazy.Value;
}
