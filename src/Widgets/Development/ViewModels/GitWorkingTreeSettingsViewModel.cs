using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Development.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Development.ViewModels;

public record EditorOption(CodeEditorType Type, string Name);

public class GitWorkingTreeSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private GitWorkingTreeModel model;

    public GitWorkingTreeSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<GitWorkingTreeModel>() ?? new GitWorkingTreeModel();

        Repositories = new ObservableCollection<string>(model.Repositories);

        RemoveFolderCommand = ReactiveCommand.Create<string>(path =>
        {
            if (Repositories.Remove(path))
            {
                Save();
            }
        });
    }

    public ObservableCollection<string> Repositories { get; }

    public List<EditorOption> AvailableEditors { get; } =
    [
        new(CodeEditorType.VSCode, "Visual Studio Code"),
        new(CodeEditorType.Cursor, "Cursor"),
        new(CodeEditorType.Windsurf, "Windsurf"),
        new(CodeEditorType.AntigravityIDE, "Antigravity IDE"),
        new(CodeEditorType.AntigravityCLI, "Antigravity CLI (agy)"),
        new(CodeEditorType.ClaudeCode, "Claude Code"),
        new(CodeEditorType.Aider, "Aider"),
        new(CodeEditorType.Rider, "JetBrains Rider"),
        new(CodeEditorType.IntelliJ, "JetBrains IntelliJ IDEA"),
        new(CodeEditorType.PyCharm, "JetBrains PyCharm"),
        new(CodeEditorType.WebStorm, "JetBrains WebStorm"),
        new(CodeEditorType.VisualStudio, "Visual Studio"),
        new(CodeEditorType.Zed, "Zed"),
        new(CodeEditorType.SublimeText, "Sublime Text"),
        new(CodeEditorType.Neovim, "Neovim"),
        new(CodeEditorType.QuickPick, "Quick Pick (Installed IDEs & AI Tools)"),
        new(CodeEditorType.SystemOpenWith, "Windows Open With... (Always Ask)"),
        new(CodeEditorType.Custom, "Custom Command...")
    ];

    public EditorOption SelectedEditor
    {
        get => AvailableEditors.FirstOrDefault(x => x.Type == model.PreferredEditor) ?? AvailableEditors[0];
        set
        {
            if (value != null && model.PreferredEditor != value.Type)
            {
                model = model with { PreferredEditor = value.Type };
                Save();
                this.RaisePropertyChanged(nameof(SelectedEditor));
                this.RaisePropertyChanged(nameof(IsCustomEditor));
            }
        }
    }

    public bool IsCustomEditor => SelectedEditor?.Type == CodeEditorType.Custom;

    public string CustomEditorCommand
    {
        get => model.CustomEditorCommand ?? "";
        set
        {
            if (model.CustomEditorCommand != value)
            {
                model = model with { CustomEditorCommand = value };
                Save();
                this.RaisePropertyChanged(nameof(CustomEditorCommand));
            }
        }
    }

    public int SelectedIndex
    {
        get => model.SelectedIndex >= 0 && model.SelectedIndex < Repositories.Count ? model.SelectedIndex : 0;
        set
        {
            if (value >= 0 && value < Repositories.Count)
            {
                model = model with { SelectedIndex = value };
                Save();
                this.RaisePropertyChanged(nameof(SelectedIndex));
            }
        }
    }

    public ICommand RemoveFolderCommand { get; }

    public void AddRepository(string path)
    {
        if (!Repositories.Contains(path))
        {
            Repositories.Add(path);
            Save();
        }
    }

    private void Save()
    {
        if (widgetLayoutProvider == null) return;
        model = model with { Repositories = Repositories.ToList() };
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
