namespace Development.Models;

public enum CodeEditorType
{
    VSCode,
    Cursor,
    Windsurf,
    AntigravityIDE,
    AntigravityCLI,
    ClaudeCode,
    Aider,
    Rider,
    IntelliJ,
    PyCharm,
    WebStorm,
    VisualStudio,
    Zed,
    SublimeText,
    Neovim,
    QuickPick,
    SystemOpenWith,
    Custom
}

public record GitWorkingTreeModel
{
    public List<string> Repositories { get; init; } = [];
    public int SelectedIndex { get; init; } = 0;
    public CodeEditorType PreferredEditor { get; init; } = CodeEditorType.VSCode;
    public string CustomEditorCommand { get; init; } = "";
}

public record GitStatusInfo(
    string RepoPath,
    string RepoName,
    string Branch,
    int Ahead,
    int Behind,
    int ModifiedCount,
    int UntrackedCount,
    bool IsClean,
    bool IsValidGitRepo);
