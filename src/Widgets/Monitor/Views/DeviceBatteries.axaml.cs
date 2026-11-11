using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Monitor.Models;
using Monitor.ViewModels;

namespace Monitor.Views;

public partial class DeviceBatteries : UserControl, IDisposable
{
    public DeviceBatteries() : this(new DeviceBatteriesModel())
    {
    }

    public DeviceBatteries(DeviceBatteriesModel model)
    {
        DataContext = new DeviceBatteriesViewModel(model);
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
