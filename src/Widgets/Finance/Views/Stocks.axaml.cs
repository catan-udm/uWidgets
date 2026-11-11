using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Stocks : UserControl, IDisposable
{
    public Stocks() : this(new StocksModel())
    {
    }

    public Stocks(StocksModel model)
    {
        DataContext = new StocksViewModel(model);
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        Dispose();
    }

    public void Dispose()
    {
        (DataContext as IDisposable)?.Dispose();
    }
}
