using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Crypto : UserControl, IDisposable
{
    public Crypto() : this(new CryptoModel())
    {
    }

    public Crypto(CryptoModel model)
    {
        DataContext = new CryptoViewModel(model);
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
