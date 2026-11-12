using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class DesktopGrid : UserControl
{
    private readonly AdvancedViewModel viewModel;

    public DesktopGrid(IAppSettingsProvider appSettingsProvider)
    {
        viewModel = new AdvancedViewModel(appSettingsProvider);
        DataContext = viewModel;
        InitializeComponent();
    }

    private void PresetCompact_OnClick(object? sender, RoutedEventArgs e) => viewModel.Size = 48;
    private void PresetDefault_OnClick(object? sender, RoutedEventArgs e) => viewModel.Size = 72;
    private void PresetLarge_OnClick(object? sender, RoutedEventArgs e) => viewModel.Size = 96;
}
