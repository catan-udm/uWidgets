using Avalonia.Controls;
using Development.ViewModels;
using uWidgets.Core.Interfaces;

namespace Development.Views.Settings;

public partial class DevAppsSettings : UserControl
{
    public DevAppsSettings() : this(null!)
    {
    }

    public DevAppsSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new DevAppsSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
