using Avalonia.Controls;
using Development.ViewModels;
using uWidgets.Core.Interfaces;

namespace Development.Views.Settings;

public partial class DockerSettings : UserControl
{
    public DockerSettings() : this(null!)
    {
    }

    public DockerSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new DockerSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
