using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace uWidgets.Core.Services;

/// <inheritdoc />
public class WidgetLayoutProvider(ILayoutProvider layoutProvider, WidgetLayout? widgetLayout) : IWidgetLayoutProvider
{
    /// <inheritdoc />
    public event DataChangedEvent<WidgetLayout>? DataChanging;

    /// <inheritdoc />
    public event DataChangedEvent<WidgetLayout>? DataChanged;

    /// <inheritdoc />
    public WidgetLayout Get() => widgetLayout!;

    /// <inheritdoc />
    public void Save(WidgetLayout data)
    {
        if (widgetLayout == null) return;

        var targetId = data.Id != Guid.Empty
            ? data.Id
            : (widgetLayout.Id != Guid.Empty ? widgetLayout.Id : Guid.NewGuid());
        var sanitizedData = data with { Id = targetId };

        DataChanging?.Invoke(this, widgetLayout, sanitizedData);
        var layout = layoutProvider.Get();
        var index = FindIndex(layout, widgetLayout);

        if (index == -1) return;

        layout[index] = sanitizedData;
        layoutProvider.Save(layout);
        var oldData = widgetLayout;
        widgetLayout = sanitizedData;
        DataChanged?.Invoke(this, oldData, sanitizedData);
    }

    /// <inheritdoc />
    public void Remove()
    {
        if (widgetLayout == null) return;

        var layout = layoutProvider.Get();
        var index = FindIndex(layout, widgetLayout);

        if (index != -1)
        {
            layout.RemoveAt(index);
            layoutProvider.Save(layout);
        }
    }

    private static int FindIndex(List<WidgetLayout> layout, WidgetLayout target)
    {
        if (target.Id != Guid.Empty)
        {
            var byId = layout.FindIndex(item => item.Id == target.Id);
            if (byId != -1) return byId;
        }

        var byCoords = layout.FindIndex(item =>
            item.Type == target.Type &&
            item.SubType == target.SubType &&
            item.X == target.X &&
            item.Y == target.Y);
        if (byCoords != -1) return byCoords;

        var byRef = layout.IndexOf(target);
        if (byRef != -1) return byRef;

        return layout.FindIndex(item => item.Type == target.Type && item.SubType == target.SubType);
    }
}
