using System;
using Microsoft.Extensions.DependencyInjection;
using uWidgets.Views;

namespace uWidgets.Services;

/// <summary>
/// Manages the single active <see cref="Settings"/> window instance across explicit and last-window-close modes.
/// </summary>
public class SettingsWindowHost(IServiceProvider serviceProvider)
{
    private Settings? current;

    public Settings? CurrentIfAlive => current;

    public Settings GetOrCreate()
    {
        if (current != null) return current;

        var instance = serviceProvider.GetRequiredService<Settings>();
        instance.Closed += OnSettingsClosed;
        current = instance;
        return instance;
    }

    private void OnSettingsClosed(object? sender, EventArgs e)
    {
        if (sender is Settings win)
        {
            win.Closed -= OnSettingsClosed;
            if (ReferenceEquals(current, win))
                current = null;
        }
    }
}
