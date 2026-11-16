using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using ReactiveUI;
using Reminders.Locales;
using Reminders.Models;

namespace Reminders.ViewModels;

public class ReminderItemViewModel(ReminderModel source) : ReactiveObject
{
    public ReminderModel Model { get; private set; } = source;
    public bool Completed => Model.Completed;
    public string Title => Model.Title;
    public TextDecorationCollection? TitleDecorations => Completed ? TextDecorations.Strikethrough : null;
    public double TitleOpacity => Completed ? 0.45 : 1.0;

    public void Update(ReminderModel updated)
    {
        Model = updated;
        this.RaisePropertyChanged(nameof(Completed));
        this.RaisePropertyChanged(nameof(Title));
        this.RaisePropertyChanged(nameof(TitleDecorations));
        this.RaisePropertyChanged(nameof(TitleOpacity));
    }
}

public class RemindersViewModel : ReactiveObject
{
    private RemindersListModel model;

    public RemindersViewModel(RemindersListModel model)
    {
        this.model = model;
        Reminders = new ObservableCollection<ReminderItemViewModel>(
            model.Reminders.Select(r => new ReminderItemViewModel(r)));
    }

    public string? ListName => model.ListName;
    public ObservableCollection<ReminderItemViewModel> Reminders { get; }
    public int Count => Reminders.Count;
    public int PendingCount => Reminders.Count(r => !r.Completed);
    public bool HasCompleted => Reminders.Any(r => r.Completed);
    public string FirstPendingTitle =>
        Reminders.FirstOrDefault(r => !r.Completed)?.Title
        ?? Reminders.FirstOrDefault()?.Title
        ?? Locale.Reminders_List_Title;

    public void ApplyModel(RemindersListModel updated)
    {
        model = updated;
        Reminders.Clear();
        foreach (var item in updated.Reminders)
            Reminders.Add(new ReminderItemViewModel(item));
        NotifySummary();
    }

    public void NotifySummary()
    {
        this.RaisePropertyChanged(nameof(ListName));
        this.RaisePropertyChanged(nameof(Count));
        this.RaisePropertyChanged(nameof(PendingCount));
        this.RaisePropertyChanged(nameof(HasCompleted));
        this.RaisePropertyChanged(nameof(FirstPendingTitle));
    }
}
