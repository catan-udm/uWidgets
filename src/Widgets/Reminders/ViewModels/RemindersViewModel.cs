using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using Reminders.Locales;
using Reminders.Models;

namespace Reminders.ViewModels;

public class RemindersViewModel(RemindersListModel model) : ReactiveObject
{
    public string? ListName => model.ListName;
    public IEnumerable<ReminderModel> Reminders => model.Reminders;
    public int Count => model.Reminders.Count;
    public int PendingCount => model.Reminders.Count(r => !r.Completed);
    public string FirstPendingTitle =>
        model.Reminders.FirstOrDefault(r => !r.Completed)?.Title
        ?? model.Reminders.FirstOrDefault()?.Title
        ?? Locale.Reminders_List_Title;
}
