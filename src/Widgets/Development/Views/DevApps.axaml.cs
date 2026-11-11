using Avalonia.Controls;
using Development.Models;
using Development.ViewModels;

namespace Development.Views;

public partial class DevApps : UserControl
{
    public DevApps() : this(new DevAppsModel())
    {
    }

    public DevApps(DevAppsModel model)
    {
        DataContext = new DevAppsViewModel(model);
        InitializeComponent();
    }
}
