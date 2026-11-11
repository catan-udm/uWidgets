using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Development.Models;
using Development.ViewModels;

namespace Development.Views;

public partial class GitWorkingTree : UserControl, IDisposable
{
    public GitWorkingTree() : this(new GitWorkingTreeModel())
    {
    }

    public GitWorkingTree(GitWorkingTreeModel model)
    {
        DataContext = new GitWorkingTreeViewModel(model);
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is GitWorkingTreeViewModel vm)
        {
            vm.IsSmallView = e.NewSize.Width <= 200 && e.NewSize.Height <= 200;
        }
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
}
