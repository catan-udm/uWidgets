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
