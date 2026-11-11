using Avalonia.Controls;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Crypto : UserControl
{
    public Crypto() : this(new CryptoModel())
    {
    }

    public Crypto(CryptoModel model)
    {
        DataContext = new CryptoViewModel(model);
        InitializeComponent();
    }
}
