using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using ReactiveUI;
using Shortcuts.Models;

namespace Shortcuts.ViewModels;

public class ShortcutItemViewModel(ShortcutItemModel item, bool showLabel, Action<string?>? onStatusMessage = null) : ReactiveObject
{
    private string? launchError;

    public string Name => item.Name;
    public string Target => item.Target;
    public bool ShowLabel => showLabel;
    public ShortcutIconOption IconOption => ShortcutIcon.FindOption(item.IconKey);
    public StreamGeometry? Icon => IconOption.Icon;
    public IImage? Image => IconOption.Image;
    public bool HasImage => IconOption.HasImage;
    public string TargetSummary => FormatTargetSummary(item.Target);
    public bool CanOpenContainingFolder => TryGetLocalTarget(out _, out _);

    public string? LaunchError
    {
        get => launchError;
        private set
        {
            this.RaiseAndSetIfChanged(ref launchError, value);
            this.RaisePropertyChanged(nameof(ToolTipText));
        }
    }

    public string ToolTipText => !string.IsNullOrWhiteSpace(LaunchError)
        ? $"{Name}\n⚠ {LaunchError}"
        : string.IsNullOrWhiteSpace(Target)
            ? Name
            : $"{Name}\n{Target}";

    public void Launch()
    {
        LaunchError = null;
        onStatusMessage?.Invoke(null);
        if (string.IsNullOrWhiteSpace(Target))
        {
            LaunchError = "No target configured.";
            onStatusMessage?.Invoke($"'{Name}': No target configured.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(Target)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LaunchError = ex.Message;
            onStatusMessage?.Invoke($"Failed to open '{Name}': {ex.Message}");
        }
    }

    public void OpenContainingFolder()
    {
        if (!TryGetLocalTarget(out var resolvedPath, out var isDirectory)) return;
        try
        {
            LaunchError = null;
            onStatusMessage?.Invoke(null);
            if (isDirectory)
            {
                Process.Start(new ProcessStartInfo(resolvedPath) { UseShellExecute = true });
            }
            else
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{resolvedPath}\"")
                {
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            LaunchError = ex.Message;
            onStatusMessage?.Invoke($"Failed to open folder: {ex.Message}");
        }
    }

    public async Task CopyTargetAsync(TopLevel? topLevel)
    {
        if (topLevel?.Clipboard == null || string.IsNullOrWhiteSpace(Target)) return;
        try
        {
            await topLevel.Clipboard.SetTextAsync(Target);
        }
        catch (Exception ex)
        {
            onStatusMessage?.Invoke($"Copy failed: {ex.Message}");
        }
    }

    private bool TryGetLocalTarget(out string resolvedPath, out bool isDirectory)
    {
        resolvedPath = string.Empty;
        isDirectory = false;
        if (string.IsNullOrWhiteSpace(Target)) return false;

        var expanded = Environment.ExpandEnvironmentVariables(Target.Trim().Trim('"'));
        if (expanded.Contains("://", StringComparison.Ordinal)) return false;

        try
        {
            if (Directory.Exists(expanded))
            {
                resolvedPath = Path.GetFullPath(expanded);
                isDirectory = true;
                return true;
            }

            if (File.Exists(expanded))
            {
                resolvedPath = Path.GetFullPath(expanded);
                return true;
            }
        }
        catch
        {
            // Invalid path characters or inaccessible target
        }

        return false;
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
    private string? statusMessage;

    public LauncherViewModel(LauncherModel model)
    {
        var items = model.Items is { Count: > 0 }
            ? model.Items
            : LauncherModel.CreateDefault().Items;

        Items = items
            .Select(x => new ShortcutItemViewModel(x, model.ShowLabels, message => StatusMessage = message))
            .ToArray();
    }

    public ShortcutItemViewModel[] Items { get; }
    public int Count => Items.Length;
    public ScrollBarVisibility SmallScrollVisibility => Count > 4 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    public ScrollBarVisibility WideScrollVisibility => Count > 4 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    public ScrollBarVisibility LargeScrollVisibility => Count > 6 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

    public string? StatusMessage
    {
        get => statusMessage;
        set => this.RaiseAndSetIfChanged(ref statusMessage, value);
    }
}
