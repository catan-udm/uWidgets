using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace uWidgets.Services;

public class WidgetPlacementService(
    IAppSettingsProvider appSettingsProvider,
    ILayoutProvider layoutProvider,
    IWidgetFactory<Window, UserControl> widgetFactory)
{
    public WidgetLayout CreateSmartLayout(Window referenceWindow, string type, string subType, int cols = 2, int rows = 2, PixelRect? avoidRect = null)
    {
        var (size, margin, _) = appSettingsProvider.Get().Dimensions;
        var width = cols * size + Math.Max(0, cols - 1) * margin;
        var height = rows * size + Math.Max(0, rows - 1) * margin;

        var screen = GetPrimaryOrFirstScreen(referenceWindow);
        var work = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        var scaling = screen?.Scaling ?? 1.0;

        var cellSpan = Math.Max(1, (int)(scaling * (size + margin)));
        var startX = work.X + (int)(scaling * margin * 2);
        var startY = work.Y + (int)(scaling * margin * 2);
        var pixelW = Math.Max(1, (int)(width * scaling));
        var pixelH = Math.Max(1, (int)(height * scaling));

        var existingRects = layoutProvider.Get()
            .Select(w => new PixelRect(w.X, w.Y, Math.Max(1, (int)(w.Width * scaling)), Math.Max(1, (int)(w.Height * scaling))))
            .ToList();

        PixelPoint? fallbackFreeSlot = null;

        for (var y = startY; y + pixelH <= work.Y + work.Height; y += cellSpan)
        {
            for (var x = startX; x + pixelW <= work.X + work.Width; x += cellSpan)
            {
                var candidate = new PixelRect(x, y, pixelW, pixelH);
                if (existingRects.Any(r => r.Intersects(candidate)))
                    continue;

                if (avoidRect.HasValue && avoidRect.Value.Intersects(candidate))
                {
                    fallbackFreeSlot ??= new PixelPoint(x, y);
                    continue;
                }

                return new WidgetLayout(Guid.NewGuid(), type, subType, x, y, width, height, null);
            }
        }

        var chosen = fallbackFreeSlot ?? new PixelPoint(startX, startY);
        return new WidgetLayout(Guid.NewGuid(), type, subType, chosen.X, chosen.Y, width, height, null);
    }

    public int SpawnStarterPack(Window referenceWindow, IReadOnlyList<(string Type, string SubType, int Cols, int Rows)> items)
    {
        var screen = GetPrimaryOrFirstScreen(referenceWindow);
        var workArea = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
        var scaling = screen?.Scaling ?? 1.0;
        var (size, margin, _) = appSettingsProvider.Get().Dimensions;
        var cellSpan = scaling * (size + margin);

        var startX = (int)(workArea.X + scaling * margin * 2);
        var startY = (int)(workArea.Y + scaling * margin * 2);
        var currentX = startX;
        var currentY = startY;
        var maxRowPixelHeight = 0;
        var spawned = 0;

        foreach (var (type, subType, cols, rows) in items)
        {
            var dipW = cols * size + Math.Max(0, cols - 1) * margin;
            var dipH = rows * size + Math.Max(0, rows - 1) * margin;
            var pixelW = (int)(dipW * scaling);
            var pixelH = (int)(dipH * scaling);

            if (currentX + pixelW > workArea.X + workArea.Width - (int)(scaling * margin * 2))
            {
                currentX = startX;
                currentY += maxRowPixelHeight + (int)(scaling * margin);
                maxRowPixelHeight = 0;
            }

            if (currentY + pixelH > workArea.Y + workArea.Height)
            {
                currentY = Math.Max(startY, (int)(workArea.Y + workArea.Height - pixelH - scaling * margin));
            }

            try
            {
                var layout = new WidgetLayout(Guid.NewGuid(), type, subType, currentX, currentY, dipW, dipH, null);
                widgetFactory.Add(layout).Show();
                spawned++;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WidgetPlacementService] Failed to spawn {type}.{subType}: {ex.Message}");
            }

            currentX += pixelW + (int)(scaling * margin);
            maxRowPixelHeight = Math.Max(maxRowPixelHeight, pixelH);
        }

        return spawned;
    }

    public static PixelRect? GetWindowPixelBounds(Window? window)
    {
        if (window == null || !window.IsVisible) return null;
        var scaling = window.Screens.ScreenFromWindow(window)?.Scaling ?? window.RenderScaling;
        return new PixelRect(
            window.Position.X,
            window.Position.Y,
            Math.Max(1, (int)(window.Width * scaling)),
            Math.Max(1, (int)(window.Height * scaling)));
    }

    private static Screen? GetPrimaryOrFirstScreen(Window window) =>
        window.Screens.Primary ?? window.Screens.All.FirstOrDefault();
}
