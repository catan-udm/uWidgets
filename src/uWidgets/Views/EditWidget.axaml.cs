using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;

namespace uWidgets.Views;

public partial class EditWidget : Window
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    public UserControl Control { get; }
    public string WidgetType => widgetLayoutProvider.Get().Type;
    public string WidgetSubType => widgetLayoutProvider.Get().SubType;
    public string WindowTitle => $"Edit {WidgetType} — {WidgetSubType}";

    public EditWidget(IWidgetLayoutProvider widgetLayoutProvider, UserControl control)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        DataContext = this;
        Control = control;
        InitializeComponent();
        Opened += OnOpenedClamp;
        KeyDown += OnKeyDown;
        Closed += OnClosed;
    }

    private void OnOpenedClamp(object? sender, EventArgs e)
    {
        var screen = Screens.ScreenFromWindow(this)
                     ?? (Owner is Window ownerWin ? Screens.ScreenFromWindow(ownerWin) : null)
                     ?? Screens.Primary;
        if (screen == null) return;

        var work = screen.WorkingArea;
        var scaling = screen.Scaling;
        var pixelW = Math.Max(1, (int)(Width * scaling));
        var pixelH = Math.Max(1, (int)(Height * scaling));
        var maxX = Math.Max(work.X, work.X + work.Width - pixelW);
        var maxY = Math.Max(work.Y, work.Y + work.Height - pixelH);
        Position = new PixelPoint(Math.Clamp(Position.X, work.X, maxX), Math.Clamp(Position.Y, work.Y, maxY));
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Opened -= OnOpenedClamp;
        KeyDown -= OnKeyDown;
        Closed -= OnClosed;
        if (Control.DataContext is IDisposable dcDisposable && !ReferenceEquals(dcDisposable, Control))
            dcDisposable.Dispose();
        if (Control is IDisposable controlDisposable)
            controlDisposable.Dispose();
    }

    private void Close(object? sender, RoutedEventArgs e) => Close();
}