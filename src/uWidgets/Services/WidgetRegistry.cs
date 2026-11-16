using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Microsoft.Win32;
using uWidgets.Views;

namespace uWidgets.Services;

public class WidgetRegistry : IDisposable
{
    private readonly List<Widget> widgets = [];
    private bool displaySubscribed;

    public WidgetRegistry()
    {
        if (OperatingSystem.IsWindows())
        {
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            displaySubscribed = true;
        }
    }

    public bool AreWidgetsHidden { get; private set; }

    public event EventHandler? WidgetsChanged;

    public IReadOnlyList<Widget> ActiveWidgets => widgets.ToList();

    public void Register(Widget widget)
    {
        if (!widgets.Contains(widget))
        {
            widgets.Add(widget);
            if (AreWidgetsHidden)
            {
                AreWidgetsHidden = false;
                InteropService.SetWatchdogPaused(false);
            }
            WidgetsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Unregister(Widget widget)
    {
        if (widgets.Remove(widget))
        {
            WidgetsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Widget? FindById(Guid id) => widgets.FirstOrDefault(w => w.WidgetId == id);

    public bool ToggleAllVisibility()
    {
        AreWidgetsHidden = !AreWidgetsHidden;
        InteropService.SetWatchdogPaused(AreWidgetsHidden);
        foreach (var widget in widgets.ToList())
        {
            if (AreWidgetsHidden)
                widget.Hide();
            else
                widget.Show();
        }
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
        return AreWidgetsHidden;
    }

    public void ShowAll()
    {
        AreWidgetsHidden = false;
        InteropService.SetWatchdogPaused(false);
        foreach (var widget in widgets.ToList())
            widget.Show();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RecoverAllOffscreen()
    {
        var recoveredCount = 0;
        foreach (var widget in widgets.ToList())
        {
            if (widget.RecoverIfOffscreen(recoveredCount))
                recoveredCount++;
        }
    }

    public void CloseAll()
    {
        foreach (var widget in widgets.ToList())
            widget.Close();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (!OperatingSystem.IsWindows()) return;
        Dispatcher.UIThread.Post(RecoverAllOffscreen, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (displaySubscribed && OperatingSystem.IsWindows())
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            displaySubscribed = false;
        }
        GC.SuppressFinalize(this);
    }
}
