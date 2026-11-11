using Avalonia.Controls;
using Finance.ViewModels;
using uWidgets.Core.Interfaces;

namespace Finance.Views.Settings;

public partial class CryptoSettings : UserControl
{
    public CryptoSettings() : this(null!)
    {
    }

    public CryptoSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new CryptoSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
