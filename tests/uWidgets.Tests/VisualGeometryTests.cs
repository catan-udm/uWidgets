using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Development.ViewModels;
using Finance.ViewModels;
using Monitor.ViewModels;
using Photos.Models;
using Photos.ViewModels;
using uWidgets.Styles;
using Xunit;

namespace uWidgets.Tests;

/// <summary>
/// Regression tests ensuring shared and widget vector geometries maintain valid bounds,
/// alignment parity, and expected view-model icon defaults.
/// </summary>
public class VisualGeometryTests
{
    private static readonly object AvaloniaInitLock = new();
    private static bool avaloniaInitialized;

    private sealed class TestAvaloniaApp : Application;

    private static void EnsureAvaloniaSkia()
    {
        if (avaloniaInitialized) return;
        lock (AvaloniaInitLock)
        {
            if (avaloniaInitialized) return;
            if (Application.Current is null)
            {
                AppBuilder.Configure<TestAvaloniaApp>()
                    .UsePlatformDetect()
                    .SetupWithoutStarting();
            }
            avaloniaInitialized = true;
        }
    }

    [Fact]
    public void WidgetIcons_PlayAndPause_HaveExactIdenticalIntegerBounds()
    {
        EnsureAvaloniaSkia();

        var playBounds = WidgetIcons.Play.Bounds;
        var pauseBounds = WidgetIcons.Pause.Bounds;

        Assert.Equal(3.0, playBounds.X, precision: 2);
        Assert.Equal(2.0, playBounds.Y, precision: 2);
        Assert.Equal(10.0, playBounds.Width, precision: 2);
        Assert.Equal(12.0, playBounds.Height, precision: 2);

        Assert.Equal(playBounds.X, pauseBounds.X, precision: 2);
        Assert.Equal(playBounds.Y, pauseBounds.Y, precision: 2);
        Assert.Equal(playBounds.Width, pauseBounds.Width, precision: 2);
        Assert.Equal(playBounds.Height, pauseBounds.Height, precision: 2);
    }

    [Fact]
    public void WidgetIcons_CatalogIcons_HavePositiveFiniteNormalizedBounds()
    {
        EnsureAvaloniaSkia();

        var icons = new (string Name, StreamGeometry Geometry)[]
        {
            (nameof(WidgetIcons.MusicNote), WidgetIcons.MusicNote),
            (nameof(WidgetIcons.Refresh), WidgetIcons.Refresh),
            (nameof(WidgetIcons.Close), WidgetIcons.Close),
            (nameof(WidgetIcons.Menu), WidgetIcons.Menu),
            (nameof(WidgetIcons.SwapHorizontal), WidgetIcons.SwapHorizontal),
            (nameof(WidgetIcons.ArrowRight), WidgetIcons.ArrowRight),
        };

        foreach (var (name, geom) in icons)
        {
            var b = geom.Bounds;
            Assert.True(b.Width > 0 && b.Height > 0, $"{name} has empty bounds: {b}");
            Assert.True(b.X >= -0.01 && b.Y >= -0.01, $"{name} has negative origin: {b}");
            Assert.True(b.Right <= 16.05 && b.Bottom <= 16.05, $"{name} exceeds 16x16 viewbox: {b}");
        }
    }

    [Fact]
    public void PhotosViewModel_ExposesSharedWidgetIcons()
    {
        EnsureAvaloniaSkia();

        using var photoFrame = new PhotoFrameViewModel(new PhotoFrameModel());
        Assert.False(photoFrame.IsPaused);
        Assert.Same(WidgetIcons.Pause, photoFrame.PauseIcon);
        Assert.Equal("Pause slideshow", photoFrame.PauseTooltip);
    }

    [Fact]
    public void DevelopmentFinanceAndMonitorIcons_HaveCleanBalancedBounds()
    {
        EnsureAvaloniaSkia();

        Assert.Same(WidgetIcons.Play, DevelopmentIcon.Play.Value);

        var sparklesBounds = DevelopmentIcon.Sparkles.Value.Bounds;
        Assert.True(sparklesBounds.X >= 0 && sparklesBounds.Y >= 0, $"Sparkles origin negative: {sparklesBounds}");
        Assert.True(sparklesBounds.Right <= 16.0 && sparklesBounds.Bottom <= 16.0, $"Sparkles exceeds 16x16: {sparklesBounds}");

        var upBounds = FinanceIcon.ArrowUp.Value.Bounds;
        var downBounds = FinanceIcon.ArrowDown.Value.Bounds;
        Assert.Equal(upBounds.Width, downBounds.Width, precision: 2);
        Assert.Equal(upBounds.Height, downBounds.Height, precision: 2);
        Assert.True(upBounds.Width >= 8.0, $"ArrowUp lost symmetry (too narrow): {upBounds}");
        Assert.Equal(8.0, upBounds.X + upBounds.Width / 2.0, precision: 2);

        var ramBounds = MetricIcon.RamUsage.Value.Bounds;
        Assert.Equal(0.0, ramBounds.X, precision: 2);
        Assert.Equal(2.0, ramBounds.Y, precision: 2);
        Assert.Equal(16.0, ramBounds.Width, precision: 2);
        Assert.Equal(12.0, ramBounds.Height, precision: 2);
    }

    [Fact]
    public void ShortcutIcons_AllEntries_FitWithinNormalizedBoundsWithoutCurveBlowup()
    {
        EnsureAvaloniaSkia();

        var shortcutType = typeof(Shortcuts.Views.Launcher).Assembly.GetType("Shortcuts.ViewModels.ShortcutIcon");
        Assert.NotNull(shortcutType);

        var dictField = shortcutType!.GetField("Icons", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(dictField);

        var icons = Assert.IsAssignableFrom<IReadOnlyDictionary<string, StreamGeometry>>(dictField!.GetValue(null));
        Assert.NotEmpty(icons);

        foreach (var kv in icons)
        {
            var b = kv.Value.Bounds;
            Assert.True(b.Width > 0 && b.Height > 0, $"Shortcut '{kv.Key}' has empty bounds: {b}");
            Assert.True(b.X >= -0.05 && b.Y >= -0.05, $"Shortcut '{kv.Key}' has negative coords: {b}");
            Assert.True(b.Right <= 17.6 && b.Bottom <= 17.6, $"Shortcut '{kv.Key}' blew out past normalized bounds: {b}");
        }
    }
}
