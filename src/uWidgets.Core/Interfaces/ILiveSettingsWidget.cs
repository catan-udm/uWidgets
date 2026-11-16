using uWidgets.Core.Models;

namespace uWidgets.Core.Interfaces;

/// <summary>
/// Implemented by interactive widgets that can update their view-model state in place
/// when <see cref="WidgetLayout.Settings"/> changes, avoiding full control tear-down.
/// </summary>
public interface ILiveSettingsWidget
{
    /// <summary>
    /// Handles a layout settings update. Return <c>true</c> if the widget updated itself in place,
    /// or <c>false</c> to let the host recreate the widget control.
    /// </summary>
    bool OnLayoutSettingsChanged(WidgetLayout newLayout);
}
