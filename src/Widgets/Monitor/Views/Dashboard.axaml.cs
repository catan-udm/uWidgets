using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Monitor.Models;
using Monitor.ViewModels;

namespace Monitor.Views;

public partial class Dashboard : UserControl, IDisposable
{
    private readonly DashboardViewModel viewModel;

    public Dashboard() : this(new DashboardModel()) { }

    public Dashboard(DashboardModel model)
    {
        viewModel = new DashboardViewModel(model);
        DataContext = viewModel;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        const int smallSize = 110;
        var small = e.NewSize is { Height: < smallSize };
        Margin = new(small ? 6 : 10);
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
}
