using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Development.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Development.ViewModels;

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
