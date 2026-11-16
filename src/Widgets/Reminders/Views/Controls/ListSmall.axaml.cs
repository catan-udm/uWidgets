using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Reminders.ViewModels;

namespace Reminders.Views.Controls;

public partial class ListSmall : UserControl
{
    public ListSmall(RemindersViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    public void ListNameChanged(object? sender, RoutedEventArgs e) => (Parent as List)?.ListNameChanged(sender, e);
    public void CompleteReminder(object? sender, RoutedEventArgs e) => (Parent as List)?.CompleteReminder(sender, e);
    public void DeleteReminder(object? sender, RoutedEventArgs e) => (Parent as List)?.DeleteReminder(sender, e);
    public void ClearCompleted(object? sender, RoutedEventArgs e) => (Parent as List)?.ClearCompleted(sender, e);
    public void EditReminder(object? sender, RoutedEventArgs e) => (Parent as List)?.EditReminder(sender, e);
    public void CreateReminder(object? sender, RoutedEventArgs e) => (Parent as List)?.CreateReminder(sender, e);
    public void CreateReminderKeyDown(object? sender, KeyEventArgs e) => (Parent as List)?.CreateReminderKeyDown(sender, e);
}