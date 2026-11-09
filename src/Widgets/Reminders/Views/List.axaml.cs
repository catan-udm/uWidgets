using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Reminders.Locales;
using Reminders.Models;
using Reminders.ViewModels;
using Reminders.Views.Controls;
using uWidgets.Core.Interfaces;

namespace Reminders.Views;

public partial class List : UserControl
{
    private RemindersListModel model;
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private RemindersViewModel viewModel;

    public List(IWidgetLayoutProvider widgetLayoutProvider) 
        : this(new RemindersListModel(Locale.Reminders_List_Title, []), widgetLayoutProvider) {}
    
    public List(RemindersListModel model, IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        this.model = model;
        viewModel = new RemindersViewModel(model);
        Content = new ListSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
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

        Content = (tiny, small, wide) switch
        {
            (true, _, _) => new ListTiny(viewModel),
            (_, true, _) => new ListSmall(viewModel),
            (_, _, true) => new ListWide(viewModel),
            _ => new ListLarge(viewModel)
        };
    }

    public void ListNameChanged(object? sender, RoutedEventArgs e)
    {
        var listName = (sender as TextBox)!.Text;
        UpdateModel(model with { ListName = listName });
    }

    private void UpdateModel(RemindersListModel newModel)
    {
        model = newModel;
        viewModel = new RemindersViewModel(newModel);
        DataContext = viewModel;
        if (Content is Control control)
        {
            control.DataContext = viewModel;
        }

        var newSettings = JsonSerializer.SerializeToElement(newModel);
        var newLayout = widgetLayoutProvider.Get() with { Settings = newSettings };
        
        widgetLayoutProvider.Save(newLayout);
    }

    public void CompleteReminder(object? sender, RoutedEventArgs e)
    {
        var reminder = (sender as Button)!.DataContext as ReminderModel;
        var index = model.Reminders.IndexOf(reminder!);
        model.Reminders[index] = model.Reminders[index] with { Completed = !model.Reminders[index].Completed };
        UpdateModel(model);
    }
    
    public void EditReminder(object? sender, RoutedEventArgs e)
    {
        var text = (sender as TextBox)!.Text;
        var reminder = (sender as TextBox)!.DataContext as ReminderModel;
        var index = model.Reminders.IndexOf(reminder!);

        if (string.IsNullOrEmpty(text))
            model.Reminders.Remove(reminder!);
        else
            model.Reminders[index] = model.Reminders[index] with { Title = (sender as TextBox)!.Text ?? "" };
        
        UpdateModel(model);
    }
    
    public void CreateReminder(object? sender, RoutedEventArgs e)
    {
        var text = (sender as TextBox)!.Text;
        
        if (string.IsNullOrEmpty(text))
            return;
        
        model.Reminders.Add(new ReminderModel(false, text));

        (sender as TextBox)!.Clear();
        UpdateModel(model);
    }
}
