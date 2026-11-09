using System;
using GlossyGlass.Rendering;
using SkiaSharp;

namespace uWidgets.Services;

public sealed class WallpaperBackdropTextures : IDisposable
{
    public required SKImage Sharp { get; init; }
    public required SKImage Blurred { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public long Version { get; init; }

    public void Dispose()
    {
        Sharp.Dispose();
        Blurred.Dispose();
    }
}

/// <summary>
/// Delegates directly to GlossyGlass.Rendering.SharedBackdropService so uWidgets and MyDockFinder
/// share a single decoded and blurred backdrop cache.
/// </summary>
public static class WallpaperBackdropService
{
    public static event EventHandler? WallpaperChanged
    {
        add => SharedBackdropService.BackdropChanged += value;
        remove => SharedBackdropService.BackdropChanged -= value;
    }

    public static BackdropTextures? GetSharedTextures(int screenWidth, int screenHeight)
    {
        return SharedBackdropService.GetWallpaperTextures(screenWidth, screenHeight);
    }

    public static void InvalidateCache()
    {
        SharedBackdropService.InvalidateCache();
        LiquidGlassRenderer.ClearSnapshotCache();
    }
}
