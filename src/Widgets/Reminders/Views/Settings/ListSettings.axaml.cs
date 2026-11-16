using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Reminders.Locales;
using Reminders.Models;
using uWidgets.Core.Interfaces;

namespace Reminders.Views.Settings;

public partial class ListSettings : UserControl
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private bool confirmDeleteAllPending;

    public ListSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        InitializeComponent();
        RefreshButtonStates();
    }

    private RemindersListModel GetSafeModel() =>
        widgetLayoutProvider.Get().GetModel<RemindersListModel>()
        ?? new RemindersListModel(Locale.Reminders_List_Title, []);

    private void RefreshButtonStates()
    {
        var model = GetSafeModel();
        DeleteCompletedButton.IsEnabled = model.Reminders.Any(r => r.Completed);
        DeleteAllButton.IsEnabled = model.Reminders.Count > 0;
    }

    private void DeleteCompleted(object? sender, RoutedEventArgs e)
    {
        var layout = widgetLayoutProvider.Get();
        var model = GetSafeModel();
        model = model with
        {
            Reminders = model.Reminders
                .Where(entry => !entry.Completed)
                .ToList()
        };

        layout = layout with { Settings = JsonSerializer.SerializeToElement(model) };
        widgetLayoutProvider.Save(layout);
        confirmDeleteAllPending = false;
        DeleteAllButton.Content = Locale.Reminders_Delete;
        RefreshButtonStates();
    }

    private void DeleteAll(object? sender, RoutedEventArgs e)
    {
        if (!confirmDeleteAllPending)
        {
            confirmDeleteAllPending = true;
            DeleteAllButton.Content = "Confirm Delete All";
            return;
        }

        confirmDeleteAllPending = false;
        var layout = widgetLayoutProvider.Get();
        var model = GetSafeModel() with { Reminders = [] };

        layout = layout with { Settings = JsonSerializer.SerializeToElement(model) };
        widgetLayoutProvider.Save(layout);
        DeleteAllButton.Content = Locale.Reminders_Delete;
        RefreshButtonStates();
    }
}