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
        SizeChanged += OnSizeChanged;
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
        SizeChanged -= OnSizeChanged;
        (DataContext as IDisposable)?.Dispose();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var size = Math.Min(DesiredSize.Width, DesiredSize.Height);
        var margin = size >= 150 ? size * 0.1 : 6;
        Margin = new Thickness(margin, margin);
    }
}