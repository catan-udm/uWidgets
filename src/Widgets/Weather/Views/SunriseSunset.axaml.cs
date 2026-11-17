using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Weather.Models;
using Weather.ViewModels;

namespace Weather.Views;

public partial class SunriseSunset : UserControl, IDisposable
{
    public SunriseSunset() : this(new ForecastModel("Cupertino", 37.3230, -122.0322, "celsius")) {}

    public SunriseSunset(ForecastModel model)
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