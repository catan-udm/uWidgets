using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace uWidgets.Core.Services;

/// <inheritdoc cref="ILayoutProvider" />
public class LayoutProvider() : JsonParser<List<WidgetLayout>>(() => Const.LayoutFile, () => []), ILayoutProvider
{
    /// <inheritdoc />
    public override List<WidgetLayout> Get()
    {
        var layouts = base.Get();
        var migrated = false;

        for (var i = 0; i < layouts.Count; i++)
        {
            if (layouts[i].Id == Guid.Empty)
            {
                layouts[i] = layouts[i] with { Id = Guid.NewGuid() };
                migrated = true;
            }
        }

        if (migrated)
            Save(layouts);

        return layouts;
    }
}