using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Crypto : UserControl, IDisposable
{
    private readonly CryptoViewModel viewModel;

    public Crypto() : this(new CryptoModel())
    {
    }

    public Crypto(CryptoModel model)
    {
        viewModel = new CryptoViewModel(model);
        DataContext = viewModel;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        viewModel.IsCompact = e.NewSize.Width < 210;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        Dispose();
    }

    public void Dispose()
    {
        SizeChanged -= OnSizeChanged;
        viewModel.Dispose();
    }
}
