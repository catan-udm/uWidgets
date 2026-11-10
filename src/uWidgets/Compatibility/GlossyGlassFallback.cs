using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        Wallpaper = 0,
        LiveScreen = 1
    }

    public record struct LiquidGlassOptions
    {
        public static LiquidGlassOptions Default => new()
        {
            Mode = BackdropMode.Wallpaper,
            CornerRadius = 24f,
            TintOpacity = 0.4f,
            IsDarkMode = true,
            RefThickness = 50.0f,
            RefFactor = 1.5f,
            RefDispersion = 160.0f,
            ShapeRadius = 0.85f,
            ShapeRoundness = 4.0f
        };

        public BackdropMode Mode { get; set; }
        public float CornerRadius { get; set; }
        public float TintOpacity { get; set; }
        public bool IsDarkMode { get; set; }
        public float RefThickness { get; set; }
        public float RefFactor { get; set; }
        public float RefDispersion { get; set; }
        public float ShapeRadius { get; set; }
        public float ShapeRoundness { get; set; }
    }
}

namespace GlossyGlass.Rendering
{
    using GlossyGlass.Configuration;

    public sealed class BackdropTextures : IDisposable
    {
        public required SKImage Sharp { get; init; }
        public required SKImage Blurred { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required long Version { get; init; }

        public bool TryAddRef() => true;
        public void Release() { }

        public void Dispose()
        {
            Sharp.Dispose();
            Blurred.Dispose();
        }
    }

    public static class SharedBackdropService
    {
        private static readonly object SyncLock = new();
        private static BackdropTextures? cachedTextures;
        private static long version = 1;

        public static event EventHandler? BackdropChanged;

        public static long CurrentVersion => Interlocked.Read(ref version);

        public static BackdropTextures? GetWallpaperTextures(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                return null;

            lock (SyncLock)
            {
                if (cachedTextures != null && cachedTextures.Width == screenWidth && cachedTextures.Height == screenHeight)
                    return cachedTextures;

                cachedTextures?.Dispose();
                cachedTextures = CreateFallbackTextures(screenWidth, screenHeight);
                return cachedTextures;
            }
        }

        public static void InvalidateCache()
        {
            lock (SyncLock)
            {
                Interlocked.Increment(ref version);
                cachedTextures?.Dispose();
                cachedTextures = null;
            }
            BackdropChanged?.Invoke(null, EventArgs.Empty);
        }

        private static BackdropTextures CreateFallbackTextures(int width, int height)
        {
            SKBitmap? wallpaper = LoadWallpaper();
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            using var sharpSurf = SKSurface.Create(info);
            sharpSurf.Canvas.Clear(new SKColor(24, 28, 36));
            if (wallpaper != null)
            {
                using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
                float scale = Math.Max((float)width / wallpaper.Width, (float)height / wallpaper.Height);
                float dstW = wallpaper.Width * scale;
                float dstH = wallpaper.Height * scale;
                float dstX = (width - dstW) * 0.5f;
                float dstY = (height - dstH) * 0.5f;
                sharpSurf.Canvas.DrawBitmap(wallpaper, new SKRect(dstX, dstY, dstX + dstW, dstY + dstH), paint);
                wallpaper.Dispose();
            }
            var sharp = sharpSurf.Snapshot();

            using var blurSurf = SKSurface.Create(info);
            using (var blurFilter = SKImageFilter.CreateBlur(6.0f, 6.0f, SKShaderTileMode.Clamp))
            using (var blurPaint = new SKPaint { ImageFilter = blurFilter, IsAntialias = true })
            {
                blurSurf.Canvas.DrawImage(sharp, 0, 0, blurPaint);
            }
            var blurred = blurSurf.Snapshot();

            return new BackdropTextures
            {
                Sharp = sharp,
                Blurred = blurred,
                Width = width,
                Height = height,
                Version = Interlocked.Read(ref version)
            };
        }

        private static SKBitmap? LoadWallpaper()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var sb = new StringBuilder(512);
                    SystemParametersInfo(0x0073, sb.Capacity, sb, 0); // SPI_GETDESKWALLPAPER
                    string path = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        return SKBitmap.Decode(stream);
                    }

                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string transcoded = Path.Combine(appData, @"Microsoft\Windows\Themes\TranscodedWallpaper");
                    if (File.Exists(transcoded))
                    {
                        using var stream = new FileStream(transcoded, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        return SKBitmap.Decode(stream);
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);
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
            if (width <= 1f || height <= 1f || textures == null)
                return;

            float r = options.CornerRadius;
            using var roundRect = new SKRoundRect(new SKRect(0, 0, width, height), r, r);
            canvas.Save();
            try
            {
                canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);

                // Draw blurred backdrop offset
                canvas.DrawImage(textures.Blurred, -offsetX / scale, -offsetY / scale);

                // Tint overlay
                byte alpha = (byte)Math.Clamp(options.TintOpacity * 255f, 0, 255);
                SKColor tint = options.IsDarkMode ? new SKColor(18, 22, 30, (byte)(alpha * 0.4f)) : new SKColor(255, 255, 255, (byte)(alpha * 0.25f));
                using var tintPaint = new SKPaint { Color = tint, Style = SKPaintStyle.Fill };
                canvas.DrawRoundRect(roundRect, tintPaint);

                // Outer border glint
                using var strokePaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, options.IsDarkMode ? (byte)40 : (byte)70),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.0f,
                    IsAntialias = true
                };
                canvas.DrawRoundRect(roundRect, strokePaint);
            }
            finally
            {
                canvas.Restore();
            }
        }
    }
}
