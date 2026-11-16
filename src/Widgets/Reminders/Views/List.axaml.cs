using System;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Reminders.Locales;
using Reminders.Models;
using Reminders.ViewModels;
using Reminders.Views.Controls;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace Reminders.Views;

public partial class List : UserControl, ILiveSettingsWidget
{
    private RemindersListModel model;
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private readonly RemindersViewModel viewModel;
    private string? currentTier;
    private bool isSelfSaving;

    public List(IWidgetLayoutProvider widgetLayoutProvider) 
        : this(new RemindersListModel(Locale.Reminders_List_Title, []), widgetLayoutProvider) {}
    
    public List(RemindersListModel model, IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        this.model = model;
        viewModel = new RemindersViewModel(model);
        DataContext = viewModel;
        currentTier = "Small";
        Content = new ListSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    public bool OnLayoutSettingsChanged(WidgetLayout newLayout)
    {
        if (isSelfSaving)
            return true;

        var incoming = newLayout.GetModel<RemindersListModel>();
        if (incoming == null)
            return false;

        model = incoming;
        viewModel.ApplyModel(incoming);
        return true;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        const int tinySize = 120;
        const int smallSize = 200;
        var tiny = e.NewSize is { Width: <= tinySize } or { Height: <= tinySize };
        var small = e.NewSize is { Width: < smallSize, Height: < smallSize };
        var wide = e.NewSize.AspectRatio >= 1.5;

        var nextTier = (tiny, small, wide) switch
        {
            (true, _, _) => "Tiny",
            (_, true, _) => "Small",
            (_, _, true) => "Wide",
            _ => "Large"
        };

        if (nextTier == currentTier) return;
        currentTier = nextTier;

        Content = nextTier switch
        {
            "Tiny" => new ListTiny(viewModel),
            "Small" => new ListSmall(viewModel),
            "Wide" => new ListWide(viewModel),
            _ => new ListLarge(viewModel)
        };
    }

    public void ListNameChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var listName = tb.Text;
        if (string.Equals(listName ?? string.Empty, model.ListName ?? string.Empty, StringComparison.Ordinal))
            return;

        model = model with { ListName = listName };
        viewModel.NotifySummary();
        SaveModelOnly();
    }

    private void SaveModelOnly()
    {
        model = model with { Reminders = viewModel.Reminders.Select(r => r.Model).ToList() };
        viewModel.NotifySummary();

        var newSettings = JsonSerializer.SerializeToElement(model);
        var newLayout = widgetLayoutProvider.Get() with { Settings = newSettings };

        isSelfSaving = true;
        try
        {
            widgetLayoutProvider.Save(newLayout);
        }
        finally
        {
            isSelfSaving = false;
        }
    }

    public void CompleteReminder(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not ReminderItemViewModel item) return;
        item.Update(item.Model with { Completed = !item.Completed });
        SaveModelOnly();
    }

    public void DeleteReminder(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not ReminderItemViewModel item) return;
        viewModel.Reminders.Remove(item);
        SaveModelOnly();
    }

    public void MoveReminderUp(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not ReminderItemViewModel item) return;
        var index = viewModel.Reminders.IndexOf(item);
        if (index <= 0) return;
        viewModel.Reminders.Move(index, index - 1);
        SaveModelOnly();
    }

    public void ClearCompleted(object? sender, RoutedEventArgs e)
    {
        for (var i = viewModel.Reminders.Count - 1; i >= 0; i--)
        {
            if (viewModel.Reminders[i].Completed)
                viewModel.Reminders.RemoveAt(i);
        }
        SaveModelOnly();
    }

    public void EditReminder(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || tb.DataContext is not ReminderItemViewModel item) return;
        var text = tb.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            viewModel.Reminders.Remove(item);
            SaveModelOnly();
            return;
        }

        if (string.Equals(text, item.Title, StringComparison.Ordinal))
            return;

        item.Update(item.Model with { Title = text });
        SaveModelOnly();
    }

    public void CreateReminder(object? sender, RoutedEventArgs e) => TryCommitNewReminder(sender as TextBox, keepFocus: false);

    public void CreateReminderKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox tb)
        {
            e.Handled = true;
            TryCommitNewReminder(tb, keepFocus: true);
        }
    }

    private void TryCommitNewReminder(TextBox? tb, bool keepFocus)
    {
        if (tb == null) return;
        var text = tb.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var added = new ReminderItemViewModel(new ReminderModel(false, text));
        viewModel.Reminders.Add(added);
        tb.Clear();
        SaveModelOnly();
        if (keepFocus)
            tb.Focus();
    }
}
