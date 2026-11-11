using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Development.Models;
using Development.ViewModels;

namespace Development.Views;

public partial class DevApps : UserControl, IDisposable
{
    public DevApps() : this(new DevAppsModel())
    {
    }

    public DevApps(DevAppsModel model)
    {
        DataContext = new DevAppsViewModel(model);
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
