using Avalonia.Controls;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Stocks : UserControl
{
    public Stocks() : this(new StocksModel())
    {
    }

    public Stocks(StocksModel model)
    {
        DataContext = new StocksViewModel(model);
        InitializeComponent();
    }
}
