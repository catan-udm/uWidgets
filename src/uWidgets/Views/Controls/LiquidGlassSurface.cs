using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Styling;
using SkiaSharp;
using uWidgets.Services;

namespace uWidgets.Views.Controls;

public sealed class LiquidGlassSurface : Control
{
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, CornerRadius>(nameof(CornerRadius), new CornerRadius(24));

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    static LiquidGlassSurface()
    {
        AffectsRender<LiquidGlassSurface>(CornerRadiusProperty, IsVisibleProperty, BoundsProperty);
    }

    public LiquidGlassSurface()
    {
        IsHitTestVisible = false;
        WallpaperBackdropService.WallpaperChanged += OnWallpaperChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        WallpaperBackdropService.WallpaperChanged -= OnWallpaperChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnWallpaperChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsVisible || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null)
            return;

        var screen = topLevel.Screens.ScreenFromWindow(topLevel) ?? topLevel.Screens.Primary;
        var scaling = screen?.Scaling ?? 1.0;
        var screenBounds = screen?.Bounds ?? new PixelRect(0, 0, 1920, 1080);

        var offsetX = (float)(topLevel.Position.X - screenBounds.X);
        var offsetY = (float)(topLevel.Position.Y - screenBounds.Y);

        var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        var opacity = 0.4;
        if (Application.Current?.Resources.TryGetResource("BackgroundOpacity", null, out var opacityObj) == true &&
            opacityObj is double d)
        {
            opacity = Math.Clamp(d, 0.0, 1.0);
        }

        var tintColor = isDark
            ? new SKColor(18, 22, 30, (byte)(opacity * 255))
            : new SKColor(255, 255, 255, (byte)(opacity * 255));

        var cornerRadius = (float)CornerRadius.TopLeft;

        context.Custom(new LiquidGlassDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            offsetX,
            offsetY,
            screenBounds.Width,
            screenBounds.Height,
            (float)scaling,
            cornerRadius,
            tintColor,
            isDark));
    }

    internal static void RenderSurface(
        SKCanvas canvas,
        WallpaperBackdropTextures textures,
        float width,
        float height,
        float screenOffsetX,
        float screenOffsetY,
        int screenWidth,
        int screenHeight,
        float dpiScale,
        float cornerRadius,
        SKColor tintColor,
        bool isDark)
    {
        if (width <= 1f || height <= 1f)
            return;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float r = Math.Clamp(cornerRadius, 0f, Math.Min(halfW, halfH));

        using var roundRect = new SKRoundRect(new SKRect(0, 0, width, height), r, r);

        canvas.Save();
        canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);

        float minDim = Math.Min(width, height);
        // Smooth curved meniscus width
        float bezelWidth = Math.Clamp(minDim * 0.18f, 16f, 26f);
        float maxBezelWarp = bezelWidth * 0.85f;

        // Subdivide widget into a fine grid (~4px per cell) for smooth analytical SDF meniscus lens warping
        int cols = Math.Clamp((int)Math.Ceiling(width / 4f), 16, 96);
        int rows = Math.Clamp((int)Math.Ceiling(height / 4f), 16, 96);

        int vCols = cols + 1;
        int vRows = rows + 1;
        int vertexCount = vCols * vRows;

        var positions = new SKPoint[vertexCount];
        var texCoords = new SKPoint[vertexCount];
        var frostColors = new SKColor[vertexCount];

        float opacityNorm = tintColor.Alpha / 255f;

        for (int iy = 0; iy < vRows; iy++)
        {
            float y = (iy / (float)rows) * height;
            for (int ix = 0; ix < vCols; ix++)
            {
                float x = (ix / (float)cols) * width;
                int idx = iy * vCols + ix;

                positions[idx] = new SKPoint(x, y);

                float px = x - halfW;
                float py = y - halfH;

                float d = SdRoundedBox(px, py, halfW, halfH, r);

                // Analytical / central-difference outward normal of the rounded rectangle
                const float eps = 1.25f;
                float dx = SdRoundedBox(px + eps, py, halfW, halfH, r) - SdRoundedBox(px - eps, py, halfW, halfH, r);
                float dy = SdRoundedBox(px, py + eps, halfW, halfH, r) - SdRoundedBox(px, py - eps, halfW, halfH, r);
                float nLen = MathF.Sqrt(dx * dx + dy * dy + 1e-6f);
                float nx = dx / nLen;
                float ny = dy / nLen;

                float edgeDist = MathF.Max(-d, 0f);
                // t = 0 on the flat interior, rising to 1 at the outer rim of the curved bezel
                float t = Math.Clamp(1f - (edgeDist / bezelWidth), 0f, 1f);

                // Bezel refraction: zero displacement (and zero slope) where the bezel meets the flat
                // interior, increasing toward the rim. The interior is left untouched so it stays clear.
                float meniscusCurve = t * t * (3f - 2f * t) * MathF.Sqrt(t);

                float warpStrength = meniscusCurve * maxBezelWarp;
                float refractX = -nx * warpStrength;
                float refractY = -ny * warpStrength;

                texCoords[idx] = ClampToScreen(
                    screenOffsetX + (x + refractX) * dpiScale,
                    screenOffsetY + (y + refractY) * dpiScale,
                    screenWidth,
                    screenHeight);

                // Light frosting confined to the outer part of the bezel; fully transparent in the interior
                float frost = t * t * t;
                byte frostAlpha = (byte)Math.Clamp(frost * 150f, 0f, 150f);
                frostColors[idx] = new SKColor(255, 255, 255, frostAlpha);
            }
        }

        int quadCount = cols * rows;
        var indices = new ushort[quadCount * 6];
        int ii = 0;
        for (int iy = 0; iy < rows; iy++)
        {
            for (int ix = 0; ix < cols; ix++)
            {
                ushort v00 = (ushort)(iy * vCols + ix);
                ushort v10 = (ushort)(iy * vCols + ix + 1);
                ushort v01 = (ushort)((iy + 1) * vCols + ix);
                ushort v11 = (ushort)((iy + 1) * vCols + ix + 1);

                indices[ii++] = v00;
                indices[ii++] = v10;
                indices[ii++] = v11;

                indices[ii++] = v00;
                indices[ii++] = v11;
                indices[ii++] = v01;
            }
        }

        using var sharpShader = textures.Sharp.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
        using var blurredShader = textures.Blurred.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);

        // 1. Sharp wallpaper everywhere: undistorted in the flat interior, refracted across the curved bezel
        using (var baseVertices = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, texCoords, null, indices))
        using (var sharpPaint = new SKPaint { Shader = sharpShader, IsAntialias = true })
        {
            canvas.DrawVertices(baseVertices, SKBlendMode.Dst, sharpPaint);
        }

        // 2. Soft frosting that fades in only toward the rounded rim (alpha 0 across the interior)
        using (var frostVertices = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, texCoords, frostColors, indices))
        using (var blurPaint = new SKPaint { Shader = blurredShader, IsAntialias = true })
        {
            canvas.DrawVertices(frostVertices, SKBlendMode.Modulate, blurPaint);
        }

        // 3. Translucent crystal glaze (preserves background colors and refraction clarity)
        byte effectiveTintAlpha = (byte)Math.Clamp(opacityNorm * (isDark ? 64f : 42f), 12f, 72f);
        using (var tintPaint = new SKPaint
        {
            Color = tintColor.WithAlpha(effectiveTintAlpha),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            canvas.DrawRect(new SKRect(0, 0, width, height), tintPaint);
        }

        // 4. Delicate diagonal liquid surface specular reflection
        using (var sheenShader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0),
                   new SKPoint(width, height),
                   [
                       new SKColor(255, 255, 255, isDark ? (byte)36 : (byte)55),
                       new SKColor(255, 255, 255, 6),
                       new SKColor(0, 0, 0, isDark ? (byte)14 : (byte)8),
                       new SKColor(255, 255, 255, isDark ? (byte)24 : (byte)38)
                   ],
                   [0f, 0.35f, 0.72f, 1f],
                   SKShaderTileMode.Clamp))
        using (var sheenPaint = new SKPaint { Shader = sheenShader, Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            canvas.DrawRect(new SKRect(0, 0, width, height), sheenPaint);
        }

        // 5. Soft feathered meniscus specular highlight on the curved corners + delicate hairline glass edge
        using (var innerSpecularRect = new SKRoundRect(new SKRect(1.2f, 1.2f, width - 1.2f, height - 1.2f), MathF.Max(0f, r - 1.2f), MathF.Max(0f, r - 1.2f)))
        using (var innerSpecularShader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0),
                   new SKPoint(width, height),
                   [
                       new SKColor(255, 255, 255, isDark ? (byte)75 : (byte)110),
                       new SKColor(255, 255, 255, 0),
                       new SKColor(255, 255, 255, 0),
                       new SKColor(255, 255, 255, isDark ? (byte)45 : (byte)70)
                   ],
                   [0f, 0.28f, 0.72f, 1f],
                   SKShaderTileMode.Clamp))
        using (var innerSpecularPaint = new SKPaint
        {
            Shader = innerSpecularShader,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.6f,
            IsAntialias = true
        })
        {
            canvas.DrawRoundRect(innerSpecularRect, innerSpecularPaint);
        }

        using (var outerGlintRect = new SKRoundRect(new SKRect(0.45f, 0.45f, width - 0.45f, height - 0.45f), MathF.Max(0f, r - 0.45f), MathF.Max(0f, r - 0.45f)))
        using (var outerGlintShader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0),
                   new SKPoint(width, height),
                   [
                       isDark ? new SKColor(255, 255, 255, 135) : new SKColor(180, 195, 215, 75),
                       isDark ? new SKColor(255, 255, 255, 32) : new SKColor(165, 180, 200, 42),
                       isDark ? new SKColor(255, 255, 255, 24) : new SKColor(160, 175, 195, 38),
                       isDark ? new SKColor(255, 255, 255, 95) : new SKColor(175, 190, 210, 65)
                   ],
                   [0f, 0.35f, 0.65f, 1f],
                   SKShaderTileMode.Clamp))
        using (var outerGlintPaint = new SKPaint
        {
            Shader = outerGlintShader,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.8f,
            IsAntialias = true
        })
        {
            canvas.DrawRoundRect(outerGlintRect, outerGlintPaint);
        }

        canvas.Restore();
    }

    private static float SdRoundedBox(float px, float py, float bx, float by, float r)
    {
        float qx = MathF.Abs(px) - bx + r;
        float qy = MathF.Abs(py) - by + r;
        float maxQx = MathF.Max(qx, 0f);
        float maxQy = MathF.Max(qy, 0f);
        float outsideDist = MathF.Sqrt(maxQx * maxQx + maxQy * maxQy);
        float insideDist = MathF.Min(MathF.Max(qx, qy), 0f);
        return insideDist + outsideDist - r;
    }

    private static SKPoint ClampToScreen(float x, float y, int screenWidth, int screenHeight)
    {
        return new SKPoint(
            Math.Clamp(x, 0f, Math.Max(0, screenWidth - 1)),
            Math.Clamp(y, 0f, Math.Max(0, screenHeight - 1)));
    }

    private sealed class LiquidGlassDrawOperation(
        Rect bounds,
        float screenOffsetX,
        float screenOffsetY,
        int screenWidth,
        int screenHeight,
        float dpiScale,
        float cornerRadius,
        SKColor tintColor,
        bool isDark) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        public void Dispose()
        {
        }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature))
                return;

            var textures = WallpaperBackdropService.GetTexturesForScreen(screenWidth, screenHeight);
            if (textures == null)
                return;

            using var lease = leaseFeature.Lease();
            RenderSurface(
                lease.SkCanvas,
                textures,
                (float)bounds.Width,
                (float)bounds.Height,
                screenOffsetX,
                screenOffsetY,
                screenWidth,
                screenHeight,
                dpiScale,
                cornerRadius,
                tintColor,
                isDark);
        }
    }
}
