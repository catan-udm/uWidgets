using Avalonia.Controls;
using Calendar.ViewModels;
using uWidgets.Core.Interfaces;

namespace Calendar.Views.Settings;

public partial class CountdownSettings : UserControl
{
    public CountdownSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new CountdownSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
