using System;
using System.Diagnostics;
using System.IO;
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
    public ShortcutIconOption IconOption => ShortcutIcon.FindOption(item.IconKey);
    public StreamGeometry? Icon => IconOption.Icon;
    public IImage? Image => IconOption.Image;
    public bool HasImage => IconOption.HasImage;
    public string TargetSummary => FormatTargetSummary(item.Target);

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

    private static string FormatTargetSummary(string rawTarget)
    {
        if (string.IsNullOrWhiteSpace(rawTarget)) return string.Empty;

        var trimmed = rawTarget.Trim().Trim('"');
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return uri.Host;
        }

        try
        {
            var fileName = Path.GetFileName(trimmed);
            return string.IsNullOrWhiteSpace(fileName) ? trimmed : fileName;
        }
        catch
        {
            return trimmed;
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
    public int Count => Items.Length;
}
