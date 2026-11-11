using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Finance.Models;
using Finance.ViewModels;

namespace Finance.Views;

public partial class Converter : UserControl, IDisposable
{
    public Converter() : this(new ConverterModel())
    {
    }

    public Converter(ConverterModel model)
    {
        DataContext = new ConverterViewModel(model);
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
