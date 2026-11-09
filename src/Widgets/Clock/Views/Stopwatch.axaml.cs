using Avalonia.Controls;
using Avalonia.Interactivity;
using Clock.ViewModels;

namespace Clock.Views;

public partial class Stopwatch : UserControl
{
    private readonly StopwatchViewModel viewModel;

    public Stopwatch()
    {
        viewModel = new StopwatchViewModel();
        DataContext = viewModel;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        LapsContainer.IsVisible = e.NewSize.Height >= 180;
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
