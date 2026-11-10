using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Styling;
using GlossyGlass.Configuration;
using GlossyGlass.Rendering;
using uWidgets.Services;

namespace uWidgets.Views.Controls;

/// <summary>
/// Hooks uWidgets directly into GlossyGlass.dll's 3-tier cached Liquid Glass engine.
/// Stationary widgets redraw via a single cached SKImage blit with zero per-frame SDF vertex math.
/// </summary>
public sealed class LiquidGlassSurface : Control
{
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, CornerRadius>(nameof(CornerRadius), new CornerRadius(24));

    public static readonly StyledProperty<float> RefThicknessProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, float>(nameof(RefThickness), 50.0f);

    public static readonly StyledProperty<float> RefFactorProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, float>(nameof(RefFactor), 1.5f);

    public static readonly StyledProperty<float> RefDispersionProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, float>(nameof(RefDispersion), 160.0f);

    public static readonly StyledProperty<float> ShapeRadiusProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, float>(nameof(ShapeRadius), 0.85f);

    public static readonly StyledProperty<float> ShapeRoundnessProperty =
        AvaloniaProperty.Register<LiquidGlassSurface, float>(nameof(ShapeRoundness), 4.0f);

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public float RefThickness
    {
        get => GetValue(RefThicknessProperty);
        set => SetValue(RefThicknessProperty, value);
    }

    public float RefFactor
    {
        get => GetValue(RefFactorProperty);
        set => SetValue(RefFactorProperty, value);
    }

    public float RefDispersion
    {
        get => GetValue(RefDispersionProperty);
        set => SetValue(RefDispersionProperty, value);
    }

    public float ShapeRadius
    {
        get => GetValue(ShapeRadiusProperty);
        set => SetValue(ShapeRadiusProperty, value);
    }

    public float ShapeRoundness
    {
        get => GetValue(ShapeRoundnessProperty);
        set => SetValue(ShapeRoundnessProperty, value);
    }

    static LiquidGlassSurface()
    {
        AffectsRender<LiquidGlassSurface>(
            CornerRadiusProperty,
            RefThicknessProperty,
            RefFactorProperty,
            RefDispersionProperty,
            ShapeRadiusProperty,
            ShapeRoundnessProperty,
            IsVisibleProperty,
            BoundsProperty);
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
        Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual);
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

        var options = LiquidGlassOptions.Default with
        {
            Mode = BackdropMode.Wallpaper,
            CornerRadius = (float)CornerRadius.TopLeft,
            TintOpacity = (float)opacity,
            IsDarkMode = isDark,
            RefThickness = RefThickness,
            RefFactor = RefFactor,
            RefDispersion = RefDispersion,
            ShapeRadius = ShapeRadius,
            ShapeRoundness = ShapeRoundness
        };

        context.Custom(new LiquidGlassDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            offsetX,
            offsetY,
            screenBounds.Width,
            screenBounds.Height,
            (float)scaling,
            options,
            SharedBackdropService.CurrentVersion));
    }

    private sealed class LiquidGlassDrawOperation(
        Rect bounds,
        float screenOffsetX,
        float screenOffsetY,
        int screenWidth,
        int screenHeight,
        float dpiScale,
        LiquidGlassOptions options,
        long backdropVersion) : ICustomDrawOperation
    {
        private readonly Rect drawBounds = bounds;
        private readonly float offsetX = screenOffsetX;
        private readonly float offsetY = screenOffsetY;
        private readonly int scrW = screenWidth;
        private readonly int scrH = screenHeight;
        private readonly float scale = dpiScale;
        private readonly LiquidGlassOptions opts = options;
        private readonly long version = backdropVersion;

        public Rect Bounds => drawBounds;

        public void Dispose()
        {
        }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other)
        {
            if (other is not LiquidGlassDrawOperation op)
                return false;

            return op.drawBounds == drawBounds
                && MathF.Abs(op.offsetX - offsetX) < 0.5f
                && MathF.Abs(op.offsetY - offsetY) < 0.5f
                && op.scrW == scrW
                && op.scrH == scrH
                && MathF.Abs(op.scale - scale) < 0.01f
                && op.opts == opts
                && op.version == version;
        }

        public void Render(ImmediateDrawingContext context)
        {
            try
            {
                if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature))
                    return;

                var textures = WallpaperBackdropService.GetSharedTextures(scrW, scrH);
                if (textures == null)
                    return;

                using var lease = leaseFeature.Lease();
                LiquidGlassRenderer.RenderCached(
                    lease.SkCanvas,
                    textures,
                    (float)drawBounds.Width,
                    (float)drawBounds.Height,
                    offsetX,
                    offsetY,
                    scrW,
                    scrH,
                    scale,
                    opts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LiquidGlassSurface] Render error: {ex}");
                try
                {
                    var fileName = System.IO.Path.Combine(uWidgets.Core.Const.CurrentFolder, "crash_log.txt");
                    System.IO.File.AppendAllText(fileName, $"[{DateTime.UtcNow:O}] LiquidGlassSurface Render error: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                }
                catch
                {
                }
            }
        }
    }
}
