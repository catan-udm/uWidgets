using System;
using System.Linq;
using Avalonia;
using Avalonia.Platform;
using uWidgets.Core.Interfaces;
using uWidgets.Views;

namespace uWidgets.Services;

public class GridService(IAppSettingsProvider appSettingsProvider) : IGridService<Widget>
{
    public void SetSize(Widget window, int columns, int rows)
    {
        window.Width = GetSize(columns);
        window.Height = GetSize(rows);
    }

    public void SnapSize(Widget window)
    {
        window.Width = SnapDimension(window.Width);
        window.Height = SnapDimension(window.Height);
    }

    public void SnapPosition(Widget window)
    {
        var settings = appSettingsProvider.Get();
        var (size, margin, _) = settings.Dimensions;
        var screen = GetTargetScreen(window);
        if (screen == null) return;

        var scaling = screen.Scaling;
        var workArea = screen.WorkingArea;
        var cellSpan = scaling * (size + margin);
        if (cellSpan <= 0) return;

        var widgetPixelWidth = window.Width * scaling;
        var widgetPixelHeight = window.Height * scaling;

        var maxUnitsX = Math.Max(0, (int)Math.Floor((workArea.Width - widgetPixelWidth - scaling * margin) / cellSpan));
        var maxUnitsY = Math.Max(0, (int)Math.Floor((workArea.Height - widgetPixelHeight - scaling * margin) / cellSpan));

        var relX = window.Position.X - workArea.X;
        var relY = window.Position.Y - workArea.Y;

        var unitX = Math.Clamp((int)Math.Round((relX - scaling * margin) / cellSpan), 0, maxUnitsX);
        var unitY = Math.Clamp((int)Math.Round((relY - scaling * margin) / cellSpan), 0, maxUnitsY);

        var snappedX = (int)(workArea.X + unitX * cellSpan + scaling * margin);
        var snappedY = (int)(workArea.Y + unitY * cellSpan + scaling * margin);

        window.Position = new PixelPoint(snappedX, snappedY);
    }

    private static Screen? GetTargetScreen(Widget window)
    {
        var currentScaling = window.Screens.ScreenFromWindow(window)?.Scaling ?? window.RenderScaling;
        var centerPoint = new PixelPoint(
            window.Position.X + (int)(window.Width * currentScaling / 2.0),
            window.Position.Y + (int)(window.Height * currentScaling / 2.0));

        return window.Screens.ScreenFromPoint(centerPoint)
               ?? window.Screens.ScreenFromWindow(window)
               ?? window.Screens.Primary
               ?? window.Screens.All.FirstOrDefault();
    }

    private int SnapDimension(double pixels, double scaling = 1.0, bool addMargin = false, int minValue = 1)
    {
        var (size, margin, _) = appSettingsProvider.Get().Dimensions;
        var units = (int)Math.Max(minValue, Math.Round(pixels / (scaling * (size + margin))));

        return GetSize(units, scaling, addMargin);
    }

    private int GetSize(int units, double scaling = 1.0, bool addMargin = false)
    {
        var (size, margin, _) = appSettingsProvider.Get().Dimensions;

        if (addMargin)
            return (int)(scaling * units * (size + margin) + scaling * margin);

        return (int)(scaling * units * (size + margin) - scaling * margin);
    }
}