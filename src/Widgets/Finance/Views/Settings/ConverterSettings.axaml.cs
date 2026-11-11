using Avalonia.Controls;
using Finance.ViewModels;
using uWidgets.Core.Interfaces;

namespace Finance.Views.Settings;

public partial class ConverterSettings : UserControl
{
    public ConverterSettings() : this(null!)
    {
    }

    public ConverterSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        DataContext = new ConverterSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
    }
}
