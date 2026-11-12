using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Calendar.Locales;
using Calendar.Models;
using Calendar.ViewModels;

namespace Calendar.Views;

public partial class Countdown : UserControl
{
    private readonly CountdownViewModel viewModel;

    public Countdown() : this((CountdownModel?)null)
    { }

    public Countdown(CountdownModel? model)
    {
        model ??= new CountdownModel(
            Locale.Calendar_Countdown_DefaultEvent,
            new DateTime(DateTime.Now.Year + 1, 1, 1),
            new DateTime(DateTime.Now.Year, 1, 1),
            true);
        viewModel = new CountdownViewModel(model);
        DataContext = viewModel;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        DateBadge.IsVisible = e.NewSize.Width >= 200;
        ProgressBar.IsVisible = e.NewSize.Height >= 110;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        Unloaded -= OnUnloaded;
        viewModel.Dispose();
    }
}
