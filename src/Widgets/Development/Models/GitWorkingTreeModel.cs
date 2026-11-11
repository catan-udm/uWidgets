namespace Development.Models;

public record GitWorkingTreeModel
{
    public List<string> Repositories { get; init; } = [];
    public int SelectedIndex { get; init; } = 0;
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
