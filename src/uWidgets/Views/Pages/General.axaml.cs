using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.Services;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class General : UserControl
{
    private readonly GeneralViewModel viewModel;

    public General(IAppSettingsProvider appSettingsProvider, HotkeyService? hotkeyService = null)
    {
        viewModel = new GeneralViewModel(appSettingsProvider, hotkeyService);
        DataContext = viewModel;
        InitializeComponent();
    }

    private void ResetHotkeys_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.ResetHotkeysToDefault();

    private void RestartNow_OnClick(object? sender, RoutedEventArgs e) =>
        AppRestartHelper.RestartApplication();
}