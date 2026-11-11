using Avalonia.Controls;
using Monitor.Models;
using Monitor.ViewModels;

namespace Monitor.Views;

public partial class DeviceBatteries : UserControl
{
    public DeviceBatteries() : this(new DeviceBatteriesModel())
    {
    }

    public DeviceBatteries(DeviceBatteriesModel model)
    {
        DataContext = new DeviceBatteriesViewModel(model);
        InitializeComponent();
    }
}
