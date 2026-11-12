using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using uWidgets.ViewModels;

namespace uWidgets.Views.Pages;

public partial class VisualEffects : UserControl
{
    private readonly VisualEffectsViewModel viewModel;

    public VisualEffects(IAppSettingsProvider appSettingsProvider)
    {
        viewModel = new VisualEffectsViewModel(appSettingsProvider);
        DataContext = viewModel;
        InitializeComponent();
    }

    private void PresetSubtle_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.ApplyPreset(30, 1.25, 80);

    private void PresetBalanced_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.ApplyPreset(50, 1.50, 160);

    private void PresetCrystal_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.ApplyPreset(75, 1.85, 250);

    private void ResetOptics_OnClick(object? sender, RoutedEventArgs e) =>
        viewModel.ApplyPreset(50, 1.50, 160);
}
