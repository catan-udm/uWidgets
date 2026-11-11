using System.IO;
using System.Windows.Input;
using Avalonia.Media;
using Development.Models;
using Development.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Development.ViewModels;

public class GitWorkingTreeViewModel : ReactiveObject, IDisposable
{
    private readonly GitWorkingTreeModel model;
    private FileSystemWatcher? watcher;

    public GitWorkingTreeViewModel(GitWorkingTreeModel model)
    {
        this.model = model;

        OpenVSCodeCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInVSCode(ActivePath));
        OpenTerminalCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInTerminal(ActivePath));
        OpenExplorerCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInExplorer(ActivePath));

        AttachWatcher();
        Update();
        TimerService.Timer5Seconds.Subscribe(Update);
    }

    private string ActivePath =>
        model.Repositories.Count > 0 && model.SelectedIndex >= 0 && model.SelectedIndex < model.Repositories.Count
            ? model.Repositories[model.SelectedIndex]
            : (model.Repositories.FirstOrDefault() ?? "");

    private string repoName = "Select Repository in Settings";
    public string RepoName
    {
        get => repoName;
        private set => this.RaiseAndSetIfChanged(ref repoName, value);
    }

    private string branch = "No Repo";
    public string Branch
    {
        get => branch;
        private set => this.RaiseAndSetIfChanged(ref branch, value);
    }

    private string syncStatus = "";
    public string SyncStatus
    {
        get => syncStatus;
        private set => this.RaiseAndSetIfChanged(ref syncStatus, value);
    }

    private string changeStatus = "Configure repo in settings";
    public string ChangeStatus
    {
        get => changeStatus;
        private set => this.RaiseAndSetIfChanged(ref changeStatus, value);
    }

    private bool hasRepo;
    public bool HasRepo
    {
        get => hasRepo;
        private set => this.RaiseAndSetIfChanged(ref hasRepo, value);
    }

    private bool isClean;
    public bool IsClean
    {
        get => isClean;
        private set => this.RaiseAndSetIfChanged(ref isClean, value);
    }

    public IBrush StatusBrush => IsClean
        ? new SolidColorBrush(Color.Parse("#10B981"))
        : new SolidColorBrush(Color.Parse("#F59E0B"));

    public ICommand OpenVSCodeCommand { get; }
    public ICommand OpenTerminalCommand { get; }
    public ICommand OpenExplorerCommand { get; }

    private void AttachWatcher()
    {
        watcher?.Dispose();
        watcher = null;

        var path = ActivePath;
        if (!string.IsNullOrEmpty(path))
        {
            watcher = GitRepositoryService.CreateWatcher(path, Update);
        }
    }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        var path = ActivePath;
        if (string.IsNullOrEmpty(path))
        {
            HasRepo = false;
            RepoName = "No Repository Configured";
            Branch = "Click settings to add repo";
            SyncStatus = "";
            ChangeStatus = "";
            IsClean = true;
            return;
        }

        var status = await GitRepositoryService.GetStatusAsync(path);
        HasRepo = status.IsValidGitRepo;
        RepoName = status.RepoName;
        Branch = status.Branch;
        IsClean = status.IsClean;

        // Sync text
        if (status.Ahead > 0 && status.Behind > 0)
            SyncStatus = $"↑{status.Ahead} ↓{status.Behind}";
        else if (status.Ahead > 0)
            SyncStatus = $"↑{status.Ahead}";
        else if (status.Behind > 0)
            SyncStatus = $"↓{status.Behind}";
        else
            SyncStatus = "synced";

        // Change text
        if (status.IsClean)
        {
            ChangeStatus = "✔ Clean";
        }
        else
        {
            var parts = new List<string>();
            if (status.ModifiedCount > 0) parts.Add($"{status.ModifiedCount} modified");
            if (status.UntrackedCount > 0) parts.Add($"{status.UntrackedCount} untracked");
            ChangeStatus = string.Join(", ", parts);
        }

        this.RaisePropertyChanged(nameof(StatusBrush));
    }

    public void Dispose()
    {
        TimerService.Timer5Seconds.Unsubscribe(Update);
        watcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
