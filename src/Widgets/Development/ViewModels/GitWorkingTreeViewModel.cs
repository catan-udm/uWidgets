using System.IO;
using System.Threading;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Development.Models;
using Development.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Development.ViewModels;

public class GitWorkingTreeViewModel : ReactiveObject, IDisposable
{
    private readonly GitWorkingTreeModel model;
    private FileSystemWatcher? watcher;
    private int isUpdating;

    public GitWorkingTreeViewModel(GitWorkingTreeModel model)
    {
        this.model = model;

        OpenEditorCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInEditor(ActivePath, model.PreferredEditor, model.CustomEditorCommand), outputScheduler: RxApp.MainThreadScheduler);
        OpenVSCodeCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInVSCode(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        OpenTerminalCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInTerminal(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        OpenExplorerCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInExplorer(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        PromptOpenWithCommand = ReactiveCommand.Create(() => GitRepositoryService.PromptOpenWith(ActivePath), outputScheduler: RxApp.MainThreadScheduler);

        CopyBranchCommand = ReactiveCommand.Create(async () =>
        {
            if (!string.IsNullOrWhiteSpace(Branch) && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(Branch);
            }
        }, outputScheduler: RxApp.MainThreadScheduler);

        CopyPathCommand = ReactiveCommand.Create(async () =>
        {
            if (!string.IsNullOrWhiteSpace(ActivePath) && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(ActivePath);
            }
        }, outputScheduler: RxApp.MainThreadScheduler);

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

    public ICommand OpenEditorCommand { get; }
    public ICommand OpenVSCodeCommand { get; }
    public ICommand OpenTerminalCommand { get; }
    public ICommand OpenExplorerCommand { get; }
    public ICommand PromptOpenWithCommand { get; }
    public ICommand CopyBranchCommand { get; }
    public ICommand CopyPathCommand { get; }

    public string EditorButtonLabel => model.PreferredEditor switch
    {
        CodeEditorType.VSCode => "Code",
        CodeEditorType.Cursor => "Cursor",
        CodeEditorType.Windsurf => "Windsurf",
        CodeEditorType.AntigravityIDE => "Antigravity",
        CodeEditorType.AntigravityCLI => "AGY",
        CodeEditorType.ClaudeCode => "Claude",
        CodeEditorType.Aider => "Aider",
        CodeEditorType.Rider => "Rider",
        CodeEditorType.IntelliJ => "IntelliJ",
        CodeEditorType.PyCharm => "PyCharm",
        CodeEditorType.WebStorm => "WebStorm",
        CodeEditorType.VisualStudio => "VS",
        CodeEditorType.Zed => "Zed",
        CodeEditorType.SublimeText => "Sublime",
        CodeEditorType.Neovim => "Neovim",
        CodeEditorType.QuickPick => "Code...",
        CodeEditorType.SystemOpenWith => "Open With",
        CodeEditorType.Custom => "Edit",
        _ => "Code"
    };

    public StreamGeometry EditorIcon => model.PreferredEditor switch
    {
        CodeEditorType.AntigravityIDE or CodeEditorType.AntigravityCLI => DevelopmentIcon.AntigravityGeometry,
        CodeEditorType.ClaudeCode or CodeEditorType.Cursor or CodeEditorType.Windsurf or CodeEditorType.Aider => DevelopmentIcon.SparklesGeometry,
        CodeEditorType.SystemOpenWith => DevelopmentIcon.OpenWithGeometry,
        CodeEditorType.Neovim => DevelopmentIcon.TerminalGeometry,
        _ => DevelopmentIcon.CodeGeometry
    };

    public string EditorTooltip => $"Open in {EditorButtonLabel}";

    private bool isSmallView;
    public bool IsSmallView
    {
        get => isSmallView;
        set => this.RaiseAndSetIfChanged(ref isSmallView, value);
    }

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
        if (Interlocked.CompareExchange(ref isUpdating, 1, 0) != 0)
            return;

        try
        {
            var path = ActivePath;
            if (string.IsNullOrEmpty(path))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    HasRepo = false;
                    RepoName = "No Repository Configured";
                    Branch = "Click settings to add repo";
                    SyncStatus = "";
                    ChangeStatus = "";
                    IsClean = true;
                    this.RaisePropertyChanged(nameof(StatusBrush));
                });
                return;
            }

            var status = await GitRepositoryService.GetStatusAsync(path);

            // Sync text
            string syncStatusText;
            if (status.Ahead > 0 && status.Behind > 0)
                syncStatusText = $"↑{status.Ahead} ↓{status.Behind}";
            else if (status.Ahead > 0)
                syncStatusText = $"↑{status.Ahead}";
            else if (status.Behind > 0)
                syncStatusText = $"↓{status.Behind}";
            else
                syncStatusText = "";

            // Change text
            string changeStatusText;
            if (status.IsClean)
            {
                changeStatusText = "✔ Clean";
            }
            else
            {
                var parts = new List<string>();
                if (status.ModifiedCount > 0) parts.Add($"{status.ModifiedCount} modified");
                if (status.UntrackedCount > 0) parts.Add($"{status.UntrackedCount} untracked");
                changeStatusText = string.Join(", ", parts);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasRepo = status.IsValidGitRepo;
                RepoName = status.RepoName;
                Branch = status.Branch;
                IsClean = status.IsClean;
                SyncStatus = syncStatusText;
                ChangeStatus = changeStatusText;
                this.RaisePropertyChanged(nameof(StatusBrush));
            });
        }
        catch (Exception)
        {
        }
        finally
        {
            Volatile.Write(ref isUpdating, 0);
        }
    }

    public void Dispose()
    {
        TimerService.Timer5Seconds.Unsubscribe(Update);
        watcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
