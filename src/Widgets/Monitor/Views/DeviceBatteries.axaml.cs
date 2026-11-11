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
        SizeChanged += OnSizeChanged;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is DeviceBatteriesViewModel vm)
        {
            vm.IsSmallView = e.NewSize.Width <= 200 && e.NewSize.Height <= 200;
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        Unloaded -= OnUnloaded;
        SizeChanged -= OnSizeChanged;
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
