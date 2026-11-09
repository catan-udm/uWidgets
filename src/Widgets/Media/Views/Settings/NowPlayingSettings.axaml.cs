using Avalonia.Controls;
using Media.ViewModels;
using uWidgets.Core.Interfaces;

namespace Media.Views.Settings;

public partial class NowPlayingSettings : UserControl
{
    public NowPlayingSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new NowPlayingSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
