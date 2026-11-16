using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clock.ViewModels;
using uWidgets.Core.Interfaces;

namespace Clock.Views;

public partial class Stopwatch : UserControl
{
    private readonly StopwatchViewModel viewModel;

    public Stopwatch() : this(null) { }

    public Stopwatch(IWidgetLayoutProvider? widgetLayoutProvider)
    {
        var id = widgetLayoutProvider?.Get()?.Id ?? Guid.Empty;
        viewModel = new StopwatchViewModel(id);
        DataContext = viewModel;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var showFullLaps = e.NewSize.Height >= 180;
        LapsContainer.IsVisible = showFullLaps;
        CompactLapBanner.IsVisible = !showFullLaps;
    }

    private void StartPause_OnClick(object? sender, RoutedEventArgs e) => viewModel.ToggleStartPause();

    private void Lap_OnClick(object? sender, RoutedEventArgs e) => viewModel.RecordLap();

    private void Reset_OnClick(object? sender, RoutedEventArgs e) => viewModel.Reset();

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
