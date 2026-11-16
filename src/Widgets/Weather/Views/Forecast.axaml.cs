using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Weather.Models;
using Weather.ViewModels;
using Weather.Views.Controls;

namespace Weather.Views;

public partial class Forecast : UserControl, IDisposable
{
    private readonly ForecastViewModel viewModel;
    private string currentTier;
    public Forecast() : this(new ForecastModel("Cupertino", 37.3230, -122.0322, "celsius")) {}
    
    public Forecast(ForecastModel model)
    {
        viewModel = new ForecastViewModel(model);
        DataContext = viewModel;
        currentTier = "Small";
        Content = new ForecastSmall(viewModel);
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        Dispose();
    }

    public void Dispose()
    {
        viewModel.Dispose();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var nextTier = e.NewSize switch
        {
            { Width: > 230, Height: > 230 } => "Large",
            { Width: > 230, Height: > 140 } => "Wide",
            { Width: > 135, Height: > 135 } => "Small",
            _ => "Tiny"
        };

        if (nextTier == currentTier) return;
        currentTier = nextTier;

        Content = nextTier switch
        {
            "Large" => new ForecastLarge(viewModel),
            "Wide" => new ForecastWide(viewModel),
            "Small" => new ForecastSmall(viewModel),
            _ => new ForecastTiny(viewModel)
        };
    }
}