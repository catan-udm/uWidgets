using System.Diagnostics;
using System.Linq;
using Avalonia.Media;
using ReactiveUI;
using Shortcuts.Models;

namespace Shortcuts.ViewModels;

public class ShortcutItemViewModel(ShortcutItemModel item, bool showLabel) : ReactiveObject
{
    public string Name => item.Name;
    public string Target => item.Target;
    public bool ShowLabel => showLabel;
    public StreamGeometry Icon => ShortcutIcon.Get(item.IconKey);

    public void Launch()
    {
        if (string.IsNullOrWhiteSpace(Target)) return;
        try
        {
            Process.Start(new ProcessStartInfo(Target)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore failed launch targets
        }
    }
}

public class LauncherViewModel : ReactiveObject
{
    public LauncherViewModel(LauncherModel model)
    {
        var items = model.Items is { Count: > 0 }
            ? model.Items
            : LauncherModel.CreateDefault().Items;

        Items = items
            .Select(x => new ShortcutItemViewModel(x, model.ShowLabels))
            .ToArray();
    }

    public ShortcutItemViewModel[] Items { get; }
}
