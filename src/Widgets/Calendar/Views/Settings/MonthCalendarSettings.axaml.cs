using Avalonia.Controls;
using Calendar.ViewModels;
using uWidgets.Core.Interfaces;

namespace Calendar.Views.Settings;

public partial class MonthCalendarSettings : UserControl
{
    public MonthCalendarSettings() : this(null!)
    {
    }

    public MonthCalendarSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new MonthCalendarSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
