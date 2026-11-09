using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SkiaSharp;

namespace uWidgets.Services;

public sealed class WallpaperBackdropTextures : IDisposable
{
    public required SKImage Sharp { get; init; }
    public required SKImage Blurred { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public void Dispose()
    {
        Sharp.Dispose();
        Blurred.Dispose();
    }
}

public static class WallpaperBackdropService
{
    private static readonly object SyncLock = new();
    private static readonly Dictionary<(int Width, int Height), WallpaperBackdropTextures> Cache = new();

    private static SKBitmap? sourceBitmap;
    private static string? cachedPath;
    private static DateTime cachedLastWriteTime;
    private static WallpaperFitMode cachedFitMode;
    private static DateTime lastCheckTime = DateTime.MinValue;

    public static event EventHandler? WallpaperChanged;

    static WallpaperBackdropService()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                SystemEvents.UserPreferenceChanged += (_, _) =>
                {
                    InvalidateCache();
                    WallpaperChanged?.Invoke(null, EventArgs.Empty);
                };
            }
            catch
            {
                // SystemEvents may throw PlatformNotSupportedException when running target net8.0 without RID;
                // EnsureSourceUpToDate() still checks wallpaper path and file timestamp automatically.
            }
        }
    }

    public static WallpaperBackdropTextures? GetTexturesForScreen(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
            return null;

        lock (SyncLock)
        {
            EnsureSourceUpToDate();

            var key = (screenWidth, screenHeight);
            if (Cache.TryGetValue(key, out var existing))
                return existing;

            var created = CreateScreenTextures(screenWidth, screenHeight);
            if (created != null)
                Cache[key] = created;

            return created;
        }
    }

    public static void InvalidateCache()
    {
        lock (SyncLock)
        {
            lastCheckTime = DateTime.MinValue;
            ClearCacheLocked();
        }
    }

    private static void EnsureSourceUpToDate()
    {
        var now = DateTime.UtcNow;
        if (sourceBitmap != null && (now - lastCheckTime).TotalSeconds < 2.0)
            return;

        lastCheckTime = now;

        var path = InteropService.GetWallpaperPath();
        var fitMode = InteropService.GetWallpaperFitMode();
        var writeTime = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            ? File.GetLastWriteTimeUtc(path)
            : DateTime.MinValue;

        if (sourceBitmap != null &&
            string.Equals(path, cachedPath, StringComparison.OrdinalIgnoreCase) &&
            writeTime == cachedLastWriteTime &&
            fitMode == cachedFitMode)
        {
            return;
        }

        ClearCacheLocked();

        cachedPath = path;
        cachedLastWriteTime = writeTime;
        cachedFitMode = fitMode;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                sourceBitmap = SKBitmap.Decode(stream);
            }
            catch
            {
                sourceBitmap = null;
            }
        }

        sourceBitmap ??= CreateFallbackBitmap();
    }

    private static SKBitmap CreateFallbackBitmap()
    {
        var bmp = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(new SKColor(32, 40, 56));
        return bmp;
    }

    private static WallpaperBackdropTextures? CreateScreenTextures(int screenWidth, int screenHeight)
    {
        if (sourceBitmap == null)
            return null;

        var imageInfo = new SKImageInfo(screenWidth, screenHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var sharpSurface = SKSurface.Create(imageInfo);
        if (sharpSurface == null)
            return null;

        DrawWallpaperFitted(sharpSurface.Canvas, sourceBitmap, screenWidth, screenHeight, cachedFitMode);
        var sharpImage = sharpSurface.Snapshot();

        using var blurredSurface = SKSurface.Create(imageInfo);
        if (blurredSurface == null)
        {
            sharpImage.Dispose();
            return null;
        }

        using (var blurFilter = SKImageFilter.CreateBlur(7.5f, 7.5f, SKShaderTileMode.Clamp))
        using (var blurPaint = new SKPaint { ImageFilter = blurFilter, IsAntialias = true })
        {
            blurredSurface.Canvas.DrawImage(sharpImage, 0, 0, blurPaint);
        }

        var blurredImage = blurredSurface.Snapshot();

        return new WallpaperBackdropTextures
        {
            Sharp = sharpImage,
            Blurred = blurredImage,
            Width = screenWidth,
            Height = screenHeight
        };
    }

    private static void DrawWallpaperFitted(
        SKCanvas canvas,
        SKBitmap wallpaper,
        int screenWidth,
        int screenHeight,
        WallpaperFitMode fitMode)
    {
        canvas.Clear(new SKColor(20, 20, 24));

        float srcW = wallpaper.Width;
        float srcH = wallpaper.Height;
        if (srcW <= 0 || srcH <= 0)
            return;

        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        switch (fitMode)
        {
            case WallpaperFitMode.Stretch:
            {
                canvas.DrawBitmap(wallpaper, new SKRect(0, 0, screenWidth, screenHeight), paint);
                break;
            }
            case WallpaperFitMode.Fit:
            {
                float scale = Math.Min(screenWidth / srcW, screenHeight / srcH);
                float dstW = srcW * scale;
                float dstH = srcH * scale;
                float dstX = (screenWidth - dstW) * 0.5f;
                float dstY = (screenHeight - dstH) * 0.5f;
                canvas.DrawBitmap(wallpaper, new SKRect(dstX, dstY, dstX + dstW, dstY + dstH), paint);
                break;
            }
            case WallpaperFitMode.Center:
            {
                float dstX = (screenWidth - srcW) * 0.5f;
                float dstY = (screenHeight - srcH) * 0.5f;
                canvas.DrawBitmap(wallpaper, new SKRect(dstX, dstY, dstX + srcW, dstY + srcH), paint);
                break;
            }
            case WallpaperFitMode.Tile:
            {
                using var shader = SKShader.CreateBitmap(wallpaper, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
                paint.Shader = shader;
                canvas.DrawRect(0, 0, screenWidth, screenHeight, paint);
                break;
            }
            case WallpaperFitMode.Fill:
            case WallpaperFitMode.Span:
            default:
            {
                float scale = Math.Max(screenWidth / srcW, screenHeight / srcH);
                float dstW = srcW * scale;
                float dstH = srcH * scale;
                float dstX = (screenWidth - dstW) * 0.5f;
                float dstY = (screenHeight - dstH) * 0.5f;
                canvas.DrawBitmap(wallpaper, new SKRect(dstX, dstY, dstX + dstW, dstY + dstH), paint);
                break;
            }
        }
    }

    private static void ClearCacheLocked()
    {
        foreach (var item in Cache.Values)
            item.Dispose();
        Cache.Clear();

        sourceBitmap?.Dispose();
        sourceBitmap = null;
    }
}
