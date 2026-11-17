using System.IO;
using System.Threading;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Development.Models;
using Development.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Development.ViewModels;

public class GitWorkingTreeViewModel : ReactiveObject, IDisposable
{
    private GitWorkingTreeModel model;
    private readonly Action<GitWorkingTreeModel>? onModelUpdated;
    private FileSystemWatcher? watcher;
    private int isUpdating;

    public GitWorkingTreeViewModel(GitWorkingTreeModel model, Action<GitWorkingTreeModel>? onModelUpdated = null)
    {
        this.model = model;
        this.onModelUpdated = onModelUpdated;

        OpenEditorCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInEditor(ActivePath, this.model.PreferredEditor, this.model.CustomEditorCommand), outputScheduler: RxApp.MainThreadScheduler);
        OpenVSCodeCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInVSCode(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        OpenTerminalCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInTerminal(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        OpenExplorerCommand = ReactiveCommand.Create(() => GitRepositoryService.OpenInExplorer(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        PromptOpenWithCommand = ReactiveCommand.Create(() => GitRepositoryService.PromptOpenWith(ActivePath), outputScheduler: RxApp.MainThreadScheduler);
        NextRepositoryCommand = ReactiveCommand.Create(() => CycleRepository(1), outputScheduler: RxApp.MainThreadScheduler);
        PrevRepositoryCommand = ReactiveCommand.Create(() => CycleRepository(-1), outputScheduler: RxApp.MainThreadScheduler);

        CopyBranchCommand = ReactiveCommand.Create(() => { }, outputScheduler: RxApp.MainThreadScheduler);
        CopyPathCommand = ReactiveCommand.Create(() => { }, outputScheduler: RxApp.MainThreadScheduler);

        AttachWatcher();
        Update();
        TimerService.Timer5Seconds.Subscribe(Update);
    }

    private int ActiveRepoIndex =>
        model.Repositories.Count == 0
            ? 0
            : Math.Clamp(model.SelectedIndex, 0, model.Repositories.Count - 1);

    public string ActivePath =>
        model.Repositories.Count > 0
            ? model.Repositories[ActiveRepoIndex]
            : "";

    public bool HasMultipleRepos => model.Repositories.Count > 1;
    public string RepoPositionDisplay => HasMultipleRepos ? $"{ActiveRepoIndex + 1}/{model.Repositories.Count}" : "";
    public bool IsQuickPickEditor => model.PreferredEditor == CodeEditorType.QuickPick;

    private string? copyFeedback;
    public string? CopyFeedback
    {
        get => copyFeedback;
        private set => this.RaiseAndSetIfChanged(ref copyFeedback, value);
    }

    public void ApplyModel(GitWorkingTreeModel updated)
    {
        model = updated;
        this.RaisePropertyChanged(nameof(ActivePath));
        this.RaisePropertyChanged(nameof(HasMultipleRepos));
        this.RaisePropertyChanged(nameof(RepoPositionDisplay));
        this.RaisePropertyChanged(nameof(IsQuickPickEditor));
        this.RaisePropertyChanged(nameof(EditorButtonLabel));
        this.RaisePropertyChanged(nameof(EditorIcon));
        this.RaisePropertyChanged(nameof(EditorTooltip));
        AttachWatcher();
        Update();
    }

    private void CycleRepository(int delta)
    {
        if (model.Repositories.Count <= 1) return;
        var nextIndex = (ActiveRepoIndex + delta + model.Repositories.Count) % model.Repositories.Count;
        model = model with { SelectedIndex = nextIndex };
        onModelUpdated?.Invoke(model);
        this.RaisePropertyChanged(nameof(ActivePath));
        this.RaisePropertyChanged(nameof(RepoPositionDisplay));
        AttachWatcher();
        Update();
    }

    public async Task CopyBranchAsync(TopLevel? topLevel)
    {
        if (string.IsNullOrWhiteSpace(Branch) || !HasRepo || topLevel?.Clipboard == null) return;
        await topLevel.Clipboard.SetTextAsync(Branch);
        await ShowCopyFeedbackAsync($"Copied '{Branch}'");
    }

    public async Task CopyPathAsync(TopLevel? topLevel)
    {
        if (string.IsNullOrWhiteSpace(ActivePath) || topLevel?.Clipboard == null) return;
        await topLevel.Clipboard.SetTextAsync(ActivePath);
        await ShowCopyFeedbackAsync("Copied path");
    }

    private async Task ShowCopyFeedbackAsync(string message)
    {
        CopyFeedback = message;
        await Task.Delay(1400);
        if (CopyFeedback == message)
            CopyFeedback = null;
    }

    public void OpenInSelectedEditor(CodeEditorType editorType) =>
        GitRepositoryService.OpenInEditor(ActivePath, editorType, model.CustomEditorCommand);

    private string repoName = "Git Working Tree";
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

    private static readonly IBrush CleanLightBrush = new SolidColorBrush(Color.Parse("#047857"));
    private static readonly IBrush CleanDarkBrush = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush DirtyLightBrush = new SolidColorBrush(Color.Parse("#B45309"));
    private static readonly IBrush DirtyDarkBrush = new SolidColorBrush(Color.Parse("#FBBF24"));

    public IBrush StatusBrush
    {
        get
        {
            var isDark = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            if (IsClean)
                return isDark ? CleanDarkBrush : CleanLightBrush;
            return isDark ? DirtyDarkBrush : DirtyLightBrush;
        }
    }

    public ICommand OpenEditorCommand { get; }
    public ICommand OpenVSCodeCommand { get; }
    public ICommand OpenTerminalCommand { get; }
    public ICommand OpenExplorerCommand { get; }
    public ICommand PromptOpenWithCommand { get; }
    public ICommand NextRepositoryCommand { get; }
    public ICommand PrevRepositoryCommand { get; }
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
                    RepoName = "Git Working Tree";
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
