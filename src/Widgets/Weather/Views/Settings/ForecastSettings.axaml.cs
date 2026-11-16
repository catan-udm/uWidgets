using Avalonia.Controls;
using Avalonia.Interactivity;
using uWidgets.Core.Interfaces;
using Weather.Models.Geocoding;
using Weather.Services;
using Weather.ViewModels;

namespace Weather.Views.Settings;

public partial class ForecastSettings : UserControl
{
    private readonly OpenMeteoWeatherProvider provider;
    
    public ForecastSettings(IWidgetLayoutProvider widgetLayoutProvider)
    {
        provider = new OpenMeteoWeatherProvider();
        DataContext = new ForecastSettingsViewModel(widgetLayoutProvider);
        InitializeComponent();
        Search.AsyncPopulator = SearchCity;
    }

    private async Task<IEnumerable<object>> SearchCity(string? query, CancellationToken token)
    {
        var trimmed = query?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length < 2)
            return [];

        return await provider.GetCitiesAsync(trimmed, token) ?? [];
    }

    private void Search_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ForecastSettingsViewModel viewModel || Search.SelectedItem is not City city) return;
        viewModel.Location = city;
    }

    private void Search_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ForecastSettingsViewModel viewModel || Search.SelectedItem is not City city) return;
        viewModel.Location = city;
    }
}