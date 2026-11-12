using System;
using System.Collections.Generic;
using System.Linq;
using uWidgets.Views;

namespace uWidgets.Services;

public class WidgetRegistry
{
    private readonly List<Widget> widgets = [];

    public bool AreWidgetsHidden { get; private set; }

    public event EventHandler? WidgetsChanged;

    public IReadOnlyList<Widget> ActiveWidgets => widgets.ToList();

    public void Register(Widget widget)
    {
        if (!widgets.Contains(widget))
        {
            widgets.Add(widget);
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
        foreach (var widget in widgets.ToList())
        {
            if (AreWidgetsHidden)
                widget.Hide();
            else
                widget.Show();
        }
        return AreWidgetsHidden;
    }

    public void ShowAll()
    {
        AreWidgetsHidden = false;
        foreach (var widget in widgets.ToList())
            widget.Show();
    }

    public void CloseAll()
    {
        foreach (var widget in widgets.ToList())
            widget.Close();
    }
}
