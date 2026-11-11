using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using ReactiveUI;
using Shortcuts.Models;
using uWidgets.Core.Interfaces;

namespace Shortcuts.ViewModels;

public class EditableShortcutViewModel : ReactiveObject
{
    private readonly Action onChanged;
    private string name;
    private string target;
    private string iconKey;

    public EditableShortcutViewModel(ShortcutItemModel model, Action onChanged)
    {
        this.onChanged = onChanged;
        name = model.Name;
        target = model.Target;
        iconKey = model.IconKey;
    }

    public IReadOnlyList<ShortcutIconOption> AvailableIcons => ShortcutIcon.All;

    public ShortcutIconOption SelectedIcon
    {
        get => ShortcutIcon.FindOption(iconKey);
        set
        {
            if (value != null && !string.Equals(value.Key, iconKey, StringComparison.OrdinalIgnoreCase))
            {
                IconKey = value.Key;
            }
        }
    }

    public string Name
    {
        get => name;
        set
        {
            this.RaiseAndSetIfChanged(ref name, value);
            onChanged();
        }
    }

    public string Target
    {
        get => target;
        set
        {
            this.RaiseAndSetIfChanged(ref target, value);
            onChanged();
        }
    }

    public string IconKey
    {
        get => iconKey;
        set
        {
            this.RaiseAndSetIfChanged(ref iconKey, value);
            this.RaisePropertyChanged(nameof(SelectedIcon));
            onChanged();
        }
    }

    public ShortcutItemModel ToModel() => new(Name, Target, IconKey);
}

public class LauncherSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private LauncherModel model;

    public LauncherSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider.Get().GetModel<LauncherModel>() ?? LauncherModel.CreateDefault();
        Items = new ObservableCollection<EditableShortcutViewModel>(
            model.Items.Select(x => new EditableShortcutViewModel(x, SyncItems)));
    }

    public ObservableCollection<EditableShortcutViewModel> Items { get; }

    public bool ShowLabels
    {
        get => model.ShowLabels;
        set => Save(model with { ShowLabels = value });
    }

    public void AddShortcut()
    {
        var item = new EditableShortcutViewModel(new ShortcutItemModel("App", "notepad.exe", "App"), SyncItems);
        Items.Add(item);
        SyncItems();
    }

    public void RemoveShortcut(EditableShortcutViewModel item)
    {
        if (Items.Count <= 1) return;
        Items.Remove(item);
        SyncItems();
    }

    private void SyncItems()
    {
        Save(model with { Items = Items.Select(x => x.ToModel()).ToList() });
    }

    private void Save(LauncherModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
