using System.Collections.Generic;

namespace Shortcuts.Models;

public record ShortcutItemModel(
    string Name,
    string Target,
    string IconKey = "App"
);

public record LauncherModel(
    List<ShortcutItemModel> Items,
    bool ShowLabels = true
)
{
    public static LauncherModel CreateDefault() => new(
        [
            new ShortcutItemModel("Explorer", "explorer.exe", "Folder"),
            new ShortcutItemModel("Browser", "https://github.com", "Globe"),
            new ShortcutItemModel("Terminal", "cmd.exe", "Terminal"),
            new ShortcutItemModel("Calculator", "calc.exe", "Calculator")
        ],
        true
    );
}
