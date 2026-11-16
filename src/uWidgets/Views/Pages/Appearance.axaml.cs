using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.Services;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class Appearance : UserControl, IDisposable
{
    private readonly AppearanceViewModel viewModel;

    public Appearance(IAppSettingsProvider appSettingsProvider)
    {
        viewModel = new AppearanceViewModel(appSettingsProvider);
        DataContext = viewModel;
        InitializeComponent();
    }

    private void RestartNow_OnClick(object? sender, RoutedEventArgs e) =>
        AppRestartHelper.RestartApplication();

    public void Dispose()
    {
        viewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}