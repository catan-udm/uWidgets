using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace uWidgets.Core.Services;

/// <inheritdoc />
public class WidgetLayoutProvider(ILayoutProvider layoutProvider, WidgetLayout? widgetLayout) : IWidgetLayoutProvider
{
    private bool isRemoved;

    /// <inheritdoc />
    public event DataChangedEvent<WidgetLayout>? DataChanging;

    /// <inheritdoc />
    public event DataChangedEvent<WidgetLayout>? DataChanged;

    /// <inheritdoc />
    public WidgetLayout Get() => widgetLayout!;

    /// <inheritdoc />
    public void Save(WidgetLayout data)
    {
        if (widgetLayout == null || isRemoved) return;

        var targetId = data.Id != Guid.Empty
            ? data.Id
            : (widgetLayout.Id != Guid.Empty ? widgetLayout.Id : Guid.NewGuid());
        var sanitizedData = data with { Id = targetId };

        if (sanitizedData.Equals(widgetLayout))
            return;

        var layout = layoutProvider.Get().ToList();
        var index = FindIndex(layout, widgetLayout);

        if (index == -1) return;

        var oldData = widgetLayout;
        DataChanging?.Invoke(this, oldData, sanitizedData);
        layout[index] = sanitizedData;
        layoutProvider.Save(layout);
        widgetLayout = sanitizedData;
        DataChanged?.Invoke(this, oldData, sanitizedData);
    }

    /// <inheritdoc />
    public void Remove()
    {
        if (widgetLayout == null || isRemoved) return;

        var layout = layoutProvider.Get().ToList();
        var index = FindIndex(layout, widgetLayout);

        isRemoved = true;
        if (index != -1)
        {
            var oldData = widgetLayout;
            DataChanging?.Invoke(this, oldData, oldData);
            layout.RemoveAt(index);
            layoutProvider.Save(layout);
            DataChanged?.Invoke(this, oldData, oldData);
        }
    }

    private static int FindIndex(List<WidgetLayout> layout, WidgetLayout target)
    {
        if (target.Id != Guid.Empty)
            return layout.FindIndex(item => item.Id == target.Id);

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
