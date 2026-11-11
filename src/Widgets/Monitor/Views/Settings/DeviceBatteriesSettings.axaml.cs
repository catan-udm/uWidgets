using Avalonia.Controls;
using Monitor.ViewModels;
using uWidgets.Core.Interfaces;

namespace Monitor.Views.Settings;

public partial class DeviceBatteriesSettings : UserControl
{
    public DeviceBatteriesSettings() : this(null!)
    {
    }

    public DeviceBatteriesSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new DeviceBatteriesSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
