using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using ReactiveUI;
using Shortcuts.Models;
using Shortcuts.Services;
using uWidgets.Core.Interfaces;

namespace Shortcuts.ViewModels;

public class EditableShortcutViewModel : ReactiveObject
{
    private readonly Action onChanged;
    private string name;
    private string target;
    private string iconKey;
    private bool canRemove = true;
    private bool canMoveUp;
    private bool canMoveDown;

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

    public bool CanRemove
    {
        get => canRemove;
        set => this.RaiseAndSetIfChanged(ref canRemove, value);
    }

    public bool CanMoveUp
    {
        get => canMoveUp;
        set => this.RaiseAndSetIfChanged(ref canMoveUp, value);
    }

    public bool CanMoveDown
    {
        get => canMoveDown;
        set => this.RaiseAndSetIfChanged(ref canMoveDown, value);
    }

    public ShortcutItemModel ToModel() => new(Name, Target, IconKey);
}

public class LauncherSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private LauncherModel model;
    private string? statusMessage;

    public LauncherSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider.Get().GetModel<LauncherModel>() ?? LauncherModel.CreateDefault();
        Items = new ObservableCollection<EditableShortcutViewModel>(
            model.Items.Select(x => new EditableShortcutViewModel(x, SyncItems)));
        AppCategories = AppCategoryService.GetCategories();
        UpdateItemFlags();
    }

    public ObservableCollection<EditableShortcutViewModel> Items { get; }

    public List<AppCategoryInfo> AppCategories { get; }

    public bool CanRemove => Items.Count > 1;

    public string? StatusMessage
    {
        get => statusMessage;
        set => this.RaiseAndSetIfChanged(ref statusMessage, value);
    }

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

    public void AddShortcutFromDiscoveredApp(DiscoveredApp app)
    {
        var item = new EditableShortcutViewModel(new ShortcutItemModel(app.Name, app.Target, app.IconKey), SyncItems);
        Items.Add(item);
        SyncItems();
    }

    public void MoveUp(EditableShortcutViewModel item)
    {
        var index = Items.IndexOf(item);
        if (index <= 0) return;
        Items.Move(index, index - 1);
        SyncItems();
    }

    public void MoveDown(EditableShortcutViewModel item)
    {
        var index = Items.IndexOf(item);
        if (index < 0 || index >= Items.Count - 1) return;
        Items.Move(index, index + 1);
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
        UpdateItemFlags();
        this.RaisePropertyChanged(nameof(CanRemove));
        Save(model with { Items = Items.Select(x => x.ToModel()).ToList() });
    }

    private void UpdateItemFlags()
    {
        var allowRemove = Items.Count > 1;
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].CanRemove = allowRemove;
            Items[i].CanMoveUp = i > 0;
            Items[i].CanMoveDown = i < Items.Count - 1;
        }
    }

    private void Save(LauncherModel updated)
    {
        model = updated;
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
