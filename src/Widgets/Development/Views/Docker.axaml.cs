using Avalonia.Controls;
using Development.Models;
using Development.ViewModels;

namespace Development.Views;

public partial class Docker : UserControl
{
    public Docker() : this(new DockerModel())
    {
    }

    public Docker(DockerModel model)
    {
        DataContext = new DockerViewModel(model);
        InitializeComponent();
    }
}
