using Avalonia.Controls;
using Reminders.ViewModels;

namespace Reminders.Views.Controls;

public partial class ListTiny : UserControl
{
    public ListTiny()
    {
        InitializeComponent();
    }

    public ListTiny(RemindersViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
