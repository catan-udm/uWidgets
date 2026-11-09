using Avalonia.Controls;
using Clock.ViewModels;
using uWidgets.Core.Interfaces;

namespace Clock.Views.Settings;

public partial class PomodoroSettings : UserControl
{
    public PomodoroSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new PomodoroSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
