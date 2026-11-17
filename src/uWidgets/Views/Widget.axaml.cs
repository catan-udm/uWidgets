using System;
using System.ComponentModel;
using System.Threading;
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

public partial class Widget : Window, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private readonly IAppSettingsProvider appSettingsProvider;
    private readonly IGridService<Widget> gridService;
    private readonly Func<UserControl> userControl;
    private readonly Func<Settings> settingsWindow;
    private readonly Func<EditWidget>? editWidgetWindow;

    private const uint WM_ENTERSIZEMOVE = 0x0231;
    private const uint WM_EXITSIZEMOVE = 0x0232;

    private bool isDragging;
    private bool isResizing;
    private bool isInSizeMove;
    private double sizeMoveStartWidth;
    private double sizeMoveStartHeight;
    private Win32Properties.CustomWndProcHookCallback? sizeMoveHook;
    private EditWidget? activeEditWindow;
    private CancellationTokenSource? highlightCts;

    public event Action<Widget>? DuplicateRequested;

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
        ScalingChanged += OnScalingChanged;
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

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        if (!IsLiquidGlassEnabled) return;
        Border.CornerRadius = Radius;
        LiquidGlassSurface.CornerRadius = Radius;
        LiquidGlassSurface.InvalidateVisual();
    }

    private void OnResized(object? sender, WindowResizedEventArgs e)
    {
        if (UseEffectiveNativeFrame && !isResizing)
            AfterResize();
        if (IsLiquidGlassEnabled)
            LiquidGlassSurface.InvalidateVisual();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Position = new PixelPoint(widgetLayoutProvider.Get().X, widgetLayoutProvider.Get().Y);
        Scale();
        InteropService.ApplyLayeringMode(this, appSettingsProvider.Get().Layout.LayeringMode);
        UpdateLiquidGlassState(appSettingsProvider.Get());
        RecoverIfOffscreen();

        if (sizeMoveHook == null && OperatingSystem.IsWindows())
        {
            sizeMoveHook = WidgetSizeMoveWndProc;
            Win32Properties.AddWndProcHookCallback(this, sizeMoveHook);
        }
    }

    internal bool IsDragging => isDragging;
    internal bool IsInSizeMove => isInSizeMove;

    private IntPtr WidgetSizeMoveWndProc(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        ProcessSizeMoveMessage(msg);
        return IntPtr.Zero;
    }

    internal void ProcessSizeMoveMessage(uint msg)
    {
        if (msg == WM_ENTERSIZEMOVE)
        {
            isInSizeMove = true;
            sizeMoveStartWidth = Width;
            sizeMoveStartHeight = Height;
        }
        else if (msg == WM_EXITSIZEMOVE)
        {
            isInSizeMove = false;
            var sizeChanged = Math.Abs(Width - sizeMoveStartWidth) > 0.5 ||
                              Math.Abs(Height - sizeMoveStartHeight) > 0.5;
            if (!isResizing && !sizeChanged && !appSettingsProvider.Get().Layout.LockPosition)
            {
                isDragging = false;
                AfterMove();
            }
        }
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (appSettingsProvider.Get().Layout.LayeringMode == LayeringMode.DesktopPinned)
            InteropService.EnforceDesktopZOrder(this);
    }

    private void DisposeContent()
    {
        if (ContentPresenter == null) return;
        var content = ContentPresenter.Content;
        if (content == null) return;

        var dc = (content as Control)?.DataContext;
        if (dc is IDisposable dcDisposable)
        {
            dcDisposable.Dispose();
        }
        if (content is IDisposable contentDisposable && !ReferenceEquals(contentDisposable, dc))
        {
            contentDisposable.Dispose();
        }
        ContentPresenter.Content = null;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        OnUnloaded(sender, new RoutedEventArgs());
    }

    private bool UseEffectiveNativeFrame => appSettingsProvider.Get().Theme.UseNativeFrame && !appSettingsProvider.Get().Theme.LiquidGlass;
    public Guid WidgetId => widgetLayoutProvider.Get().Id;
    public WidgetLayout CurrentLayout => widgetLayoutProvider.Get();
    public bool ShowEditButton => editWidgetWindow != null;
    public string Edit => $"{Locale.Widget_Edit} \"{widgetLayoutProvider.Get().Type}\"";
    public CornerRadius Radius => UseEffectiveNativeFrame
        ? new(0)
        : new((IsLiquidGlassEnabled ? Math.Max(appSettingsProvider.Get().Dimensions.Radius, 22) : appSettingsProvider.Get().Dimensions.Radius) / (Screens.ScreenFromWindow(this)?.Scaling ?? 1.0));
    public bool IsLiquidGlassEnabled => appSettingsProvider.Get().Theme.LiquidGlass;
    
    public SystemDecorations WidgetSystemDecorations => UseEffectiveNativeFrame
        ? SystemDecorations.BorderOnly
        : SystemDecorations.None;

    public bool ToolTipVisible => !appSettingsProvider.Get().Layout.LockSize;
    public bool WidgetExtendClientArea => UseEffectiveNativeFrame;
    public bool IsPositionLocked => appSettingsProvider.Get().Layout.LockPosition;
    public bool IsSizeLocked => appSettingsProvider.Get().Layout.LockSize;
    public bool CanResizeWidget => !IsSizeLocked;
    public bool CanMoveWidget => !IsPositionLocked;

    private (int Cols, int Rows) GetCurrentGridUnits()
    {
        var dims = appSettingsProvider.Get().Dimensions;
        var span = Math.Max(1, dims.Size + dims.Margin);
        var cols = Math.Max(1, (int)Math.Round((Width + dims.Margin) / span));
        var rows = Math.Max(1, (int)Math.Round((Height + dims.Margin) / span));
        return (cols, rows);
    }

    public bool IsSmallSize => GetCurrentGridUnits() == (2, 2);
    public bool IsMediumSize => GetCurrentGridUnits() == (4, 2);
    public bool IsLargeSize => GetCurrentGridUnits() == (4, 4);
    public bool IsExtraLargeSize => GetCurrentGridUnits() == (8, 4);

    public void ToggleLockPosition()
    {
        var current = appSettingsProvider.Get();
        appSettingsProvider.Save(current with { Layout = current.Layout with { LockPosition = !current.Layout.LockPosition } });
    }

    public void ToggleLockSize()
    {
        var current = appSettingsProvider.Get();
        appSettingsProvider.Save(current with { Layout = current.Layout with { LockSize = !current.Layout.LockSize } });
    }

    public void DuplicateWidget() => DuplicateRequested?.Invoke(this);

    public async void EditWidget(Window? fallbackOwner = null)
    {
        if (editWidgetWindow == null) return;
        if (activeEditWindow != null)
        {
            activeEditWindow.Activate();
            return;
        }

        var dialog = editWidgetWindow.Invoke();
        activeEditWindow = dialog;
        try
        {
            var owner = IsVisible ? this : (fallbackOwner ?? settingsWindow.Invoke());
            await dialog.ShowDialog(owner);
        }
        finally
        {
            activeEditWindow = null;
            if (IsVisible && appSettingsProvider.Get().Layout.LayeringMode == LayeringMode.DesktopPinned)
                InteropService.EnforceDesktopZOrder(this);
        }
    }

    public void EditWidget() => EditWidget(null);
    public void ResizeSmall() => _ = Resize(2, 2);
    public void ResizeMedium() => _ = Resize(4, 2);
    public void ResizeLarge() => _ = Resize(4, 4);
    public void ResizeExtraLarge() => _ = Resize(8, 4);
    public void OpenSettings()
    {
        var settings = settingsWindow.Invoke();
        settings.Show();
        if (settings.WindowState == WindowState.Minimized)
            settings.WindowState = WindowState.Normal;
        settings.Activate();
    }

    public void ResetToCenter() => ResetToCenter(0);

    public void ResetToCenter(int cascadeIndex)
    {
        var primary = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (primary == null) return;

        var work = primary.WorkingArea;
        var scaling = primary.Scaling;
        var pixelW = (int)(Width * scaling);
        var pixelH = (int)(Height * scaling);
        var offset = (cascadeIndex % 6) * Math.Max(16, (int)(28 * scaling));
        var maxX = Math.Max(work.X, work.X + work.Width - pixelW);
        var maxY = Math.Max(work.Y, work.Y + work.Height - pixelH);
        var targetX = Math.Clamp(work.X + Math.Max(0, (work.Width - pixelW) / 2) + offset, work.X, maxX);
        var targetY = Math.Clamp(work.Y + Math.Max(0, (work.Height - pixelH) / 2) + offset, work.Y, maxY);

        Position = new PixelPoint(targetX, targetY);
        AfterMove();
    }

    public bool RecoverIfOffscreen(int cascadeIndex = 0)
    {
        var screens = Screens.All;
        if (screens.Count == 0) return false;

        var scaling = Screens.ScreenFromWindow(this)?.Scaling ?? RenderScaling;
        var widgetRect = new PixelRect(
            Position.X,
            Position.Y,
            Math.Max(1, (int)(Width * scaling)),
            Math.Max(1, (int)(Height * scaling)));

        var isVisibleOnAnyScreen = false;
        foreach (var screen in screens)
        {
            var intersection = screen.WorkingArea.Intersect(widgetRect);
            if (intersection.Width >= 32 && intersection.Height >= 32)
            {
                isVisibleOnAnyScreen = true;
                break;
            }
        }

        if (!isVisibleOnAnyScreen)
        {
            ResetToCenter(cascadeIndex);
            return true;
        }

        return false;
    }

    public async Task HighlightAsync()
    {
        highlightCts?.Cancel();
        highlightCts?.Dispose();
        var cts = new CancellationTokenSource();
        highlightCts = cts;

        if (!IsVisible)
            Show();

        var wasTopmost = Topmost;
        try
        {
            Topmost = true;
            for (var i = 0; i < 3 && !cts.IsCancellationRequested; i++)
            {
                Opacity = 0.35;
                await Task.Delay(140, cts.Token);
                Opacity = 1.0;
                await Task.Delay(140, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            Opacity = 1.0;
            Topmost = wasTopmost;
            InteropService.ApplyLayeringMode(this, appSettingsProvider.Get().Layout.LayeringMode);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        CompletePointerRelease();

    internal void CompletePointerRelease()
    {
        // BeginMoveDrag runs asynchronously on Windows; avoid double-snapping while modal move loop owns input.
        if (!isDragging || isInSizeMove) return;
        isDragging = false;
        AfterMove();
    }

    private void Scale()
    {
        var scaleFactor = appSettingsProvider.Get().Dimensions.Size / 72.0;
        ContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
        ContentPresenter.VerticalAlignment = VerticalAlignment.Top;
        ContentPresenter.Width = Width / scaleFactor;
        ContentPresenter.Height = Height / scaleFactor;
        if (Math.Abs(scaleFactor - 1.0) < 0.01)
        {
            ContentPresenter.RenderTransform = null;
            return;
        }
        
        ContentPresenter.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        ContentPresenter.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
    }

    private void OnAppSettingsUpdated(object sender, AppSettings? oldData, AppSettings newData)
    {
        if (oldData?.Layout.LockSize != newData.Layout.LockSize)
        {
            SetMinMaxSize(newData.Layout.LockSize);
            OnPropertyChanged(nameof(IsSizeLocked));
            OnPropertyChanged(nameof(CanResizeWidget));
            OnPropertyChanged(nameof(ToolTipVisible));
        }

        if (oldData?.Layout.LockPosition != newData.Layout.LockPosition)
        {
            OnPropertyChanged(nameof(IsPositionLocked));
            OnPropertyChanged(nameof(CanMoveWidget));
        }

        if (oldData?.Layout.LayeringMode != newData.Layout.LayeringMode)
            InteropService.ApplyLayeringMode(this, newData.Layout.LayeringMode);

        if (oldData != null && oldData.Dimensions != newData.Dimensions)
        {
            var oldSpan = Math.Max(1, oldData.Dimensions.Size + oldData.Dimensions.Margin);
            var cols = Math.Max(1, (int)Math.Round((Width + oldData.Dimensions.Margin) / oldSpan));
            var rows = Math.Max(1, (int)Math.Round((Height + oldData.Dimensions.Margin) / oldSpan));
            SetMinMaxSize(newData.Layout.LockSize);
            gridService.SetSize(this, cols, rows);
            if (!newData.Layout.LockPosition)
                AfterMove();
            AfterResize();
            NotifySizePresetProperties();
        }

        UpdateLiquidGlassState(newData);
    }

    private void NotifySizePresetProperties()
    {
        OnPropertyChanged(nameof(IsSmallSize));
        OnPropertyChanged(nameof(IsMediumSize));
        OnPropertyChanged(nameof(IsLargeSize));
        OnPropertyChanged(nameof(IsExtraLargeSize));
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
        highlightCts?.Cancel();
        highlightCts?.Dispose();
        highlightCts = null;
        DisposeContent();
        InteropService.UnpinWindowFromDesktop(this);
        if (sizeMoveHook != null)
        {
            Win32Properties.RemoveWndProcHookCallback(this, sizeMoveHook);
            sizeMoveHook = null;
        }
        PointerPressed -= OnPointerPressed;
        PointerReleased -= OnPointerReleased;
        PositionChanged -= OnPositionChanged;
        ScalingChanged -= OnScalingChanged;
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
        var oldJson = oldLayout?.Settings?.GetRawText();
        var newJson = newLayout.Settings?.GetRawText();
        if (oldJson != newJson)
        {
            if (ContentPresenter.Content is ILiveSettingsWidget liveWidget &&
                liveWidget.OnLayoutSettingsChanged(newLayout))
            {
                return;
            }

            DisposeContent();
            ContentPresenter.Content = userControl();
        }
    }

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e) =>
        BeginDragFromPointer(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed, () => BeginMoveDrag(e));

    internal void BeginDragFromPointer(bool isLeftButtonPressed, Action startMoveDrag)
    {
        if (appSettingsProvider.Get().Layout.LockPosition) return;

        ToolTip.SetIsOpen(this, false);
        if (!isLeftButtonPressed) return;

        // Window.BeginMoveDrag posts SC_MOVE asynchronously on Win32; completion is handled via WM_EXITSIZEMOVE / PointerReleased.
        isDragging = true;
        startMoveDrag();
    }

    private void AfterMove()
    {
        var appSettings = appSettingsProvider.Get();

        if (appSettings.Layout.SnapPosition)
            gridService.SnapPosition(this);

        var settings = widgetLayoutProvider.Get();
        if (settings.X != Position.X || settings.Y != Position.Y)
            widgetLayoutProvider.Save(settings with { X = Position.X, Y = Position.Y });
    }

    private async Task Resize(int columns, int rows)
    {
        if (appSettingsProvider.Get().Layout.LockSize) return;

        Border.Transitions = new Transitions
        {
            new DoubleTransition { Property = WidthProperty, Duration = TimeSpan.FromMilliseconds(300) },
            new DoubleTransition { Property = HeightProperty, Duration = TimeSpan.FromMilliseconds(300) }
        };
        gridService.SetSize(this, columns, rows);
        ClampToCurrentWorkingArea();
        AfterResize();
        await Task.Delay(300);
        Border.Transitions = null;
    }

    private void ClampToCurrentWorkingArea()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null) return;

        var work = screen.WorkingArea;
        var scaling = screen.Scaling;
        var pixelW = Math.Max(1, (int)(Width * scaling));
        var pixelH = Math.Max(1, (int)(Height * scaling));
        var maxX = Math.Max(work.X, work.X + work.Width - pixelW);
        var maxY = Math.Max(work.Y, work.Y + work.Height - pixelH);
        var clampedX = Math.Clamp(Position.X, work.X, maxX);
        var clampedY = Math.Clamp(Position.Y, work.Y, maxY);
        if (clampedX != Position.X || clampedY != Position.Y)
            Position = new PixelPoint(clampedX, clampedY);
    }

    private void AfterResize()
    {
        if (appSettingsProvider.Get().Layout.SnapSize)
            gridService.SnapSize(this);
        
        Scale();
        var settings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(settings with
        {
            X = Position.X,
            Y = Position.Y,
            Width = (int)Width,
            Height = (int)Height
        });
        NotifySizePresetProperties();
    }

    public void Remove()
    {
        widgetLayoutProvider.Remove();
        Close();
    }

    private void Resize(object? sender, PointerPressedEventArgs e)
    {
        if (appSettingsProvider.Get().Layout.LockSize) return;

        isResizing = true;
        CanResize = true;
        try
        {
            BeginResizeDrag(WindowEdge.SouthEast, e);
        }
        finally
        {
            isResizing = false;
            CanResize = false;
        }
        AfterResize();
        e.Handled = true;
    }
}