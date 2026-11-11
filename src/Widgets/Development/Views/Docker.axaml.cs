using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Development.Models;
using Development.ViewModels;

namespace Development.Views;

public partial class Docker : UserControl, IDisposable
{
    public Docker() : this(new DockerModel())
    {
    }

    public Docker(DockerModel model)
    {
        DataContext = new DockerViewModel(model);
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
