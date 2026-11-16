using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Weather.Models;
using Weather.ViewModels;

namespace Weather.Views;

public partial class UVIndex : UserControl, IDisposable
{
    public UVIndex() : this(new ForecastModel("Cupertino", 37.3230, -122.0322, "celsius")) {}

    public UVIndex(ForecastModel model)
    {
        DataContext = new ForecastViewModel(model);
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