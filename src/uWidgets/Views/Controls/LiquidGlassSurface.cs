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
            ? new SKColor(32, 34, 40, (byte)(opacity * 255))
            : new SKColor(246, 249, 255, (byte)(opacity * 255));

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
        float bezelWidth = Math.Clamp(minDim * 0.25f, 20f, 38f);

        // Subdivide widget into a fine grid (~4px per cell) for smooth analytical SDF meniscus lens warping
        int cols = Math.Clamp((int)Math.Ceiling(width / 4f), 16, 96);
        int rows = Math.Clamp((int)Math.Ceiling(height / 4f), 16, 96);

        int vCols = cols + 1;
        int vRows = rows + 1;
        int vertexCount = vCols * vRows;

        var positions = new SKPoint[vertexCount];
        var texCoordsG = new SKPoint[vertexCount];
        var texCoordsR = new SKPoint[vertexCount];
        var texCoordsB = new SKPoint[vertexCount];
        var rimColors = new SKColor[vertexCount];

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
                float t = Math.Clamp(1f - (edgeDist / bezelWidth), 0f, 1f);

                // Smooth Bezier meniscus curve + steep outer rim lip matching MyDockFinder Liquid Glass
                float t2 = t * t;
                float bevelCurve = t2 * (1.65f - 0.65f * t);
                float rimLip = t2 * t2;

                // Subtle interior dome magnification + strong curved bezel lens refraction
                float normX = px / MathF.Max(halfW, 1f);
                float normY = py / MathF.Max(halfH, 1f);
                float domeX = -normX * 3.5f * (1f - bevelCurve);
                float domeY = -normY * 3.5f * (1f - bevelCurve);

                float warpStrength = bevelCurve * 20f + rimLip * 18f;
                float refractX = -nx * warpStrength + domeX;
                float refractY = -ny * warpStrength + domeY;

                // Chromatic dispersion at the curved glass rim
                float disp = 1f + bevelCurve * 0.16f;
                float invDisp = 2f - disp;

                texCoordsG[idx] = ClampToScreen(
                    screenOffsetX + (x + refractX) * dpiScale,
                    screenOffsetY + (y + refractY) * dpiScale,
                    screenWidth,
                    screenHeight);

                texCoordsR[idx] = ClampToScreen(
                    screenOffsetX + (x + refractX * invDisp) * dpiScale,
                    screenOffsetY + (y + refractY * invDisp) * dpiScale,
                    screenWidth,
                    screenHeight);

                texCoordsB[idx] = ClampToScreen(
                    screenOffsetX + (x + refractX * disp) * dpiScale,
                    screenOffsetY + (y + refractY * disp) * dpiScale,
                    screenWidth,
                    screenHeight);

                // Vertex alpha weight for clear refractive rim vs frosted interior
                byte clearRimAlpha = (byte)Math.Clamp((bevelCurve * 0.65f + rimLip * 0.35f) * 255f, 0f, 255f);
                rimColors[idx] = new SKColor(255, 255, 255, clearRimAlpha);
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

        float opacityNorm = tintColor.Alpha / 255f;

        // 1. Base warped backdrop (blend between sharp and frosted wallpaper based on opacity)
        using (var baseVertices = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, texCoordsG, null, indices))
        using (var sharpPaint = new SKPaint { Shader = sharpShader, IsAntialias = true })
        {
            canvas.DrawVertices(baseVertices, SKBlendMode.Dst, sharpPaint);

            byte blurAlpha = (byte)Math.Clamp((0.22f + opacityNorm * 0.58f) * 255f, 0f, 235f);
            using var blurPaint = new SKPaint
            {
                Shader = blurredShader,
                Color = new SKColor(255, 255, 255, blurAlpha),
                IsAntialias = true
            };
            canvas.DrawVertices(baseVertices, SKBlendMode.Dst, blurPaint);
        }

        // 2. Crisp refractive glass rim overlay + subtle chromatic dispersion along the bevel
        using (var rimVerticesG = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, texCoordsG, rimColors, indices))
        using (var rimPaintG = new SKPaint { Shader = sharpShader, IsAntialias = true })
        {
            canvas.DrawVertices(rimVerticesG, SKBlendMode.Modulate, rimPaintG);
        }

        // Subtle red/blue prismatic fringe along the outer curved bezel
        using (var rimVerticesR = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, texCoordsR, rimColors, indices))
        using (var rimVerticesB = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, texCoordsB, rimColors, indices))
        using (var redFilter = SKColorFilter.CreateColorMatrix(
        [
            1f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0.16f, 0f
        ]))
        using (var blueFilter = SKColorFilter.CreateColorMatrix(
        [
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 1f, 0f, 0f,
            0f, 0f, 0f, 0.16f, 0f
        ]))
        using (var dispPaintR = new SKPaint { Shader = sharpShader, ColorFilter = redFilter, BlendMode = SKBlendMode.Plus, IsAntialias = true })
        using (var dispPaintB = new SKPaint { Shader = sharpShader, ColorFilter = blueFilter, BlendMode = SKBlendMode.Plus, IsAntialias = true })
        {
            canvas.DrawVertices(rimVerticesR, SKBlendMode.Modulate, dispPaintR);
            canvas.DrawVertices(rimVerticesB, SKBlendMode.Modulate, dispPaintB);
        }

        // 3. Theme tint layer (preserves glass clarity at the bevel while applying Dark/Light tint)
        byte effectiveTintAlpha = (byte)Math.Clamp(tintColor.Alpha * 0.62f, 0f, 215f);
        if (effectiveTintAlpha > 0)
        {
            using var tintPaint = new SKPaint
            {
                Color = tintColor.WithAlpha(effectiveTintAlpha),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(new SKRect(0, 0, width, height), tintPaint);
        }

        // 4. Inner glass bevel caustic sheen & depth gradient
        using (var sheenShader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0),
                   new SKPoint(width, height),
                   [
                       new SKColor(255, 255, 255, isDark ? (byte)42 : (byte)75),
                       new SKColor(255, 255, 255, 8),
                       new SKColor(0, 0, 0, isDark ? (byte)28 : (byte)18),
                       new SKColor(255, 255, 255, isDark ? (byte)28 : (byte)52)
                   ],
                   [0f, 0.35f, 0.75f, 1f],
                   SKShaderTileMode.Clamp))
        using (var sheenPaint = new SKPaint { Shader = sheenShader, Style = SKPaintStyle.Fill, IsAntialias = true })
        {
            canvas.DrawRect(new SKRect(0, 0, width, height), sheenPaint);
        }

        // 5. Inner 3D glass thickness rim (bright inner caustic lip + subtle dark meniscus shadow)
        using (var innerShadowRect = new SKRoundRect(new SKRect(1.5f, 1.5f, width - 1.5f, height - 1.5f), MathF.Max(0f, r - 1.5f), MathF.Max(0f, r - 1.5f)))
        using (var innerShadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, isDark ? (byte)48 : (byte)32),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.0f,
            IsAntialias = true
        })
        {
            canvas.DrawRoundRect(innerShadowRect, innerShadowPaint);
        }

        using (var innerBevelRect = new SKRoundRect(new SKRect(3.0f, 3.0f, width - 3.0f, height - 3.0f), MathF.Max(0f, r - 3.0f), MathF.Max(0f, r - 3.0f)))
        using (var innerBevelShader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0),
                   new SKPoint(width, height),
                   [
                       new SKColor(255, 255, 255, isDark ? (byte)85 : (byte)150),
                       new SKColor(255, 255, 255, 18),
                       new SKColor(255, 255, 255, isDark ? (byte)55 : (byte)105)
                   ],
                   [0f, 0.5f, 1f],
                   SKShaderTileMode.Clamp))
        using (var innerBevelPaint = new SKPaint
        {
            Shader = innerBevelShader,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.75f,
            IsAntialias = true
        })
        {
            canvas.DrawRoundRect(innerBevelRect, innerBevelPaint);
        }

        // 6. Crisp outer specular glass rim highlight + subtle glass edge contour for bright wallpapers
        using (var outerRimRect = new SKRoundRect(new SKRect(0.75f, 0.75f, width - 0.75f, height - 0.75f), MathF.Max(0f, r - 0.75f), MathF.Max(0f, r - 0.75f)))
        using (var outerRimShader = SKShader.CreateLinearGradient(
                   new SKPoint(0, 0),
                   new SKPoint(width, height),
                   [
                       isDark ? new SKColor(255, 255, 255, 210) : new SKColor(150, 165, 185, 155),
                       isDark ? new SKColor(255, 255, 255, 70) : new SKColor(130, 145, 165, 95),
                       isDark ? new SKColor(255, 255, 255, 50) : new SKColor(120, 135, 155, 85),
                       isDark ? new SKColor(255, 255, 255, 165) : new SKColor(145, 160, 180, 140)
                   ],
                   [0f, 0.35f, 0.65f, 1f],
                   SKShaderTileMode.Clamp))
        using (var outerRimPaint = new SKPaint
        {
            Shader = outerRimShader,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true
        })
        {
            canvas.DrawRoundRect(outerRimRect, outerRimPaint);
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
