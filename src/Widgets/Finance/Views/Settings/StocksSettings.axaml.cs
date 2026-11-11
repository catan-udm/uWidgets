using Avalonia.Controls;
using Finance.ViewModels;
using uWidgets.Core.Interfaces;

namespace Finance.Views.Settings;

public partial class StocksSettings : UserControl
{
    public StocksSettings() : this(null!)
    {
    }

    public StocksSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new StocksSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
