using Avalonia.Controls;
using Monitor.ViewModels;
using uWidgets.Core.Interfaces;

namespace Monitor.Views.Settings;

public partial class DashboardSettings : UserControl
{
    public DashboardSettings() : this(null!)
    {
    }

    public DashboardSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new DashboardSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
