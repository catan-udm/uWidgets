using System;
using SkiaSharp;

namespace GlossyGlass
{
    public static class GlossyGlassApi
    {
        public static void StartEngine()
        {
        }
    }
}

namespace GlossyGlass.Configuration
{
    public enum BackdropMode
    {
        Wallpaper = 0
    }

    public sealed record LiquidGlassOptions
    {
        public static LiquidGlassOptions Default { get; } = new();

        public BackdropMode Mode { get; init; } = BackdropMode.Wallpaper;
        public float CornerRadius { get; init; }
        public float TintOpacity { get; init; }
        public bool IsDarkMode { get; init; }
        public float RefThickness { get; init; }
        public float RefFactor { get; init; }
        public float RefDispersion { get; init; }
        public float ShapeRadius { get; init; }
        public float ShapeRoundness { get; init; }
    }
}

namespace GlossyGlass.Rendering
{
    using GlossyGlass.Configuration;

    public sealed class BackdropTextures
    {
    }

    public static class SharedBackdropService
    {
        public static event EventHandler? BackdropChanged
        {
            add { }
            remove { }
        }

        public static long CurrentVersion => 0;

        public static BackdropTextures? GetWallpaperTextures(int screenWidth, int screenHeight)
        {
            return null;
        }

        public static void InvalidateCache()
        {
        }
    }

    public static class LiquidGlassRenderer
    {
        public static void ClearSnapshotCache()
        {
        }

        public static void RenderCached(
            SKCanvas canvas,
            BackdropTextures textures,
            float width,
            float height,
            float offsetX,
            float offsetY,
            int scrW,
            int scrH,
            float scale,
            LiquidGlassOptions options)
        {
        }
    }
}
