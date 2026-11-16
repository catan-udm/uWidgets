using System;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Development.Models;
using Development.Services;
using Development.ViewModels;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace Development.Views;

public partial class GitWorkingTree : UserControl, IDisposable, ILiveSettingsWidget
{
    private readonly IWidgetLayoutProvider? widgetLayoutProvider;
    private readonly GitWorkingTreeViewModel viewModel;
    private bool isSelfSaving;

    public GitWorkingTree() : this(new GitWorkingTreeModel())
    {
    }

    public GitWorkingTree(GitWorkingTreeModel model)
    {
        viewModel = new GitWorkingTreeViewModel(model);
        InitializeWidget();
    }

    public GitWorkingTree(IWidgetLayoutProvider widgetLayoutProvider, GitWorkingTreeModel? model = null)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        var initialModel = model ?? widgetLayoutProvider.Get()?.GetModel<GitWorkingTreeModel>() ?? new GitWorkingTreeModel();
        viewModel = new GitWorkingTreeViewModel(initialModel, SaveModelSelection);
        InitializeWidget();
    }

    private void InitializeWidget()
    {
        DataContext = viewModel;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        InitializeComponent();
    }

    public bool OnLayoutSettingsChanged(WidgetLayout newLayout)
    {
        if (isSelfSaving) return true;
        var updated = newLayout.GetModel<GitWorkingTreeModel>();
        if (updated == null) return false;
        viewModel.ApplyModel(updated);
        return true;
    }

    private void SaveModelSelection(GitWorkingTreeModel updated)
    {
        if (widgetLayoutProvider == null) return;
        var current = widgetLayoutProvider.Get();
        if (current == null) return;

        isSelfSaving = true;
        try
        {
            widgetLayoutProvider.Save(current with { Settings = JsonSerializer.SerializeToElement(updated) });
        }
        finally
        {
            isSelfSaving = false;
        }
    }

    private async void CopyBranch_OnClick(object? sender, RoutedEventArgs e) =>
        await viewModel.CopyBranchAsync(TopLevel.GetTopLevel(this));

    private async void CopyPath_OnClick(object? sender, RoutedEventArgs e) =>
        await viewModel.CopyPathAsync(TopLevel.GetTopLevel(this));

    private void Editor_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!viewModel.IsQuickPickEditor)
        {
            if (viewModel.OpenEditorCommand.CanExecute(null))
            {
                viewModel.OpenEditorCommand.Execute(null);
            }
            return;
        }

        if (sender is not Control anchor) return;
        var flyout = new MenuFlyout();
        foreach (var (editorType, label) in GitRepositoryService.GetInstalledQuickPickEditors())
        {
            var menuItem = new MenuItem { Header = label };
            menuItem.Click += (_, _) => viewModel.OpenInSelectedEditor(editorType);
            flyout.Items.Add(menuItem);
        }
        flyout.ShowAt(anchor);
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        viewModel.IsSmallView = e.NewSize.Width <= 200 && e.NewSize.Height <= 200;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        Dispose();
    }

    public void Dispose()
    {
        SizeChanged -= OnSizeChanged;
        viewModel.Dispose();
    }
}
