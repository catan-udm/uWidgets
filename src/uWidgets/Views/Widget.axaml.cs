using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Models.Settings;
using uWidgets.Locales;
using uWidgets.Services;

namespace uWidgets.Views;

public partial class Widget : Window
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly IGridService<Widget> gridService;
    private readonly Func<UserControl> userControl;
    private readonly Func<Settings> settingsWindow;
    private readonly Func<EditWidget>? editWidgetWindow;

    public Widget(IAppSettingsProvider appSettingsProvider, IWidgetLayoutProvider widgetLayoutProvider, 
        IGridService<Widget> gridService, Func<UserControl> userControl, Func<Settings> settingsWindow, 
        Func<EditWidget>? editWidgetWindow = null)
    {
        this.settingsWindow = settingsWindow;
        this.editWidgetWindow = editWidgetWindow;
        this.widgetLayoutProvider = widgetLayoutProvider;
        this.userControl = userControl;
        this.appSettingsProvider = appSettingsProvider;
        this.gridService = gridService;
        
        InitializeComponent();
        
        Height = widgetLayoutProvider.Get().Height;
        Width = widgetLayoutProvider.Get().Width;
        Title = $"{widgetLayoutProvider.Get().Type} {widgetLayoutProvider.Get().SubType}";
        ContentPresenter.Content = userControl();
        DataContext = this;
        
        SetMinMaxSize(this.appSettingsProvider.Get().Layout.LockSize);
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);
        UpdateLiquidGlassState(this.appSettingsProvider.Get());
        
        Opened += OnOpened;
        Activated += OnActivated;
        Closed += OnClosed;
        PositionChanged += OnPositionChanged;
        Resized += OnResized;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        widgetLayoutProvider.DataChanged += OnWidgetLayoutUpdated;
        appSettingsProvider.DataChanged += OnAppSettingsUpdated;
        Unloaded += OnUnloaded;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (IsLiquidGlassEnabled)
            LiquidGlassSurface.InvalidateVisual();
    }

    private void OnResized(object? sender, WindowResizedEventArgs e)
    {
        if (appSettingsProvider.Get().Theme.UseNativeFrame)
            AfterResize();
        if (IsLiquidGlassEnabled)
            LiquidGlassSurface.InvalidateVisual();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Position = new PixelPoint(widgetLayoutProvider.Get().X, widgetLayoutProvider.Get().Y);
        Scale();
        InteropService.PinWindowToDesktop(this);
        UpdateLiquidGlassState(appSettingsProvider.Get());
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        Position = new PixelPoint(widgetLayoutProvider.Get().X, widgetLayoutProvider.Get().Y);
        Scale();
        InteropService.PinWindowToDesktop(this);
        UpdateLiquidGlassState(appSettingsProvider.Get());
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        InteropService.UnpinWindowFromDesktop(this);
    }

    private bool UseEffectiveNativeFrame => appSettingsProvider.Get().Theme.UseNativeFrame && !appSettingsProvider.Get().Theme.LiquidGlass;
    public bool ShowEditButton => editWidgetWindow != null;
    public string Edit => $"{Locale.Widget_Edit} \"{widgetLayoutProvider.Get().Type}\"";
    public CornerRadius Radius => UseEffectiveNativeFrame
        ? new(0)
        : new((IsLiquidGlassEnabled ? Math.Max(appSettingsProvider.Get().Dimensions.Radius, 22) : appSettingsProvider.Get().Dimensions.Radius) / (Screens.ScreenFromWindow(this)?.Scaling ?? 1.0));
    public bool IsLiquidGlassEnabled => appSettingsProvider.Get().Theme.LiquidGlass;
    
    public SystemDecorations WidgetSystemDecorations => UseEffectiveNativeFrame
        ? SystemDecorations.BorderOnly
        : SystemDecorations.None;

    public bool ToolTipVisible => !UseEffectiveNativeFrame && !appSettingsProvider.Get().Layout.LockSize;
    public bool WidgetExtendClientArea => UseEffectiveNativeFrame;
    public async void EditWidget()
    {
        if (editWidgetWindow == null) return;
        await editWidgetWindow.Invoke().ShowDialog(this);
        InteropService.EnforceDesktopZOrder(this);
    }
    public void ResizeSmall() => _ = Resize(2, 2);
    public void ResizeMedium() => _ = Resize(4, 2);
    public void ResizeLarge() => _ = Resize(4, 4);
    public void ResizeExtraLarge() => _ = Resize(8, 4);
    public void OpenSettings() => settingsWindow.Invoke().Show();

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => AfterMove();

    private void Scale()
    {
        var scaleFactor = appSettingsProvider.Get().Dimensions.Size / 72.0;
        ContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        ContentPresenter.VerticalAlignment = VerticalAlignment.Top;
        ContentPresenter.Width = Width / scaleFactor;
        ContentPresenter.Height = Height / scaleFactor;
        if (Math.Abs(scaleFactor - 1.0) < 0.01) return;
        
        ContentPresenter.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        ContentPresenter.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
    }

    private void OnAppSettingsUpdated(object sender, AppSettings? oldData, AppSettings newData)
    {
        if (oldData?.Layout.LockSize != newData.Layout.LockSize)
            SetMinMaxSize(newData.Layout.LockSize);

        if (oldData?.Dimensions != newData.Dimensions)
        {
            AfterMove();
            AfterResize();
        }

        UpdateLiquidGlassState(newData);
    }

    private void UpdateLiquidGlassState(AppSettings settings)
    {
        var enabled = settings.Theme.LiquidGlass;
        Classes.Set("LiquidGlass", enabled);
        SystemDecorations = WidgetSystemDecorations;
        ExtendClientAreaToDecorationsHint = WidgetExtendClientArea;
        TransparencyLevelHint = enabled
            ? [WindowTransparencyLevel.Transparent]
            : settings.Theme.OpacityLevel < 1.0
                ? [WindowTransparencyLevel.AcrylicBlur]
                : [WindowTransparencyLevel.None];
        Border.CornerRadius = Radius;
        Border.ClipToBounds = !enabled;
        Border.BorderThickness = new Thickness(0);
        LiquidGlassSurface.IsVisible = enabled;
        LiquidGlassSurface.CornerRadius = Radius;
        LiquidGlassSurface.RefThickness = (float)settings.Theme.RefThickness;
        LiquidGlassSurface.RefFactor = (float)settings.Theme.RefFactor;
        LiquidGlassSurface.RefDispersion = (float)settings.Theme.RefDispersion;
        LiquidGlassSurface.ShapeRadius = (float)settings.Theme.ShapeRadius;
        LiquidGlassSurface.ShapeRoundness = (float)settings.Theme.ShapeRoundness;
        InteropService.SetLiquidGlassWindowAttributes(this, enabled);
        if (enabled)
            LiquidGlassSurface.InvalidateVisual();
    }

    private void SetMinMaxSize(bool lockSize)
    {
        var size = appSettingsProvider.Get().Dimensions.Size;
        
        MinWidth = lockSize ? Width : size;
        MinHeight = lockSize ? Height : size;
        MaxWidth = lockSize ? Width : double.PositiveInfinity;
        MaxHeight = lockSize ? Height : double.PositiveInfinity;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        InteropService.UnpinWindowFromDesktop(this);
        PointerPressed -= OnPointerPressed;
        PointerReleased -= OnPointerReleased;
        PositionChanged -= OnPositionChanged;
        Resized -= OnResized;
        Opened -= OnOpened;
        Activated -= OnActivated;
        Closed -= OnClosed;
        Unloaded -= OnUnloaded;
        widgetLayoutProvider.DataChanged -= OnWidgetLayoutUpdated;
        appSettingsProvider.DataChanged -= OnAppSettingsUpdated;
    }

    private void OnWidgetLayoutUpdated(object? sender, WidgetLayout? oldLayout, WidgetLayout newLayout)
    {
        if (!Equals(oldLayout?.Settings, newLayout.Settings))
            ContentPresenter.Content = userControl();
    }

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (appSettingsProvider.Get().Layout.LockPosition) return;
        
        ToolTip.SetIsOpen(this, false);
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;
        
        BeginMoveDrag(e);
    }

    private void AfterMove()
    {
        var appSettings = appSettingsProvider.Get();
        
        if (appSettings.Layout.SnapPosition) 
            gridService.SnapPosition(this);
        
        var settings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(settings with { X = Position.X, Y = Position.Y });
    }

    private async Task Resize(int columns, int rows)
    {
        Border.Transitions = new Transitions
        {
            new DoubleTransition { Property = WidthProperty, Duration = TimeSpan.FromMilliseconds(300) },
            new DoubleTransition { Property = HeightProperty, Duration = TimeSpan.FromMilliseconds(300) }
        };
        gridService.SetSize(this, columns, rows);
        AfterResize();
        await Task.Delay(300);
        Border.Transitions = null;
    }

    private void AfterResize()
    {
        if (appSettingsProvider.Get().Layout.SnapSize)
            gridService.SnapSize(this);
        
        Scale();
        var settings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(settings with { Width = (int)Width, Height = (int)Height });
    }

    public void Remove()
    {
        widgetLayoutProvider.Remove();
        Close();
    }

    private void Resize(object? sender, PointerPressedEventArgs e)
    {
        if (appSettingsProvider.Get().Layout.LockSize) return;

        CanResize = true;
        BeginResizeDrag(WindowEdge.SouthEast, e);
        AfterResize();
        e.Handled = true;
        CanResize = false;
    }
}