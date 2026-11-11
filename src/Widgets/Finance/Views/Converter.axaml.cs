using Avalonia.Controls;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Converter : UserControl
{
    public Converter() : this(new ConverterModel())
    {
    }

    public Converter(ConverterModel model)
    {
        DataContext = new ConverterViewModel(model);
        InitializeComponent();
    }
}
