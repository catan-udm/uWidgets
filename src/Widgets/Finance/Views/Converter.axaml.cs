using System;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Finance.Models;
using Finance.ViewModels;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;

namespace Finance.Views;

public partial class Converter : UserControl, IDisposable, ILiveSettingsWidget
{
    private readonly IWidgetLayoutProvider? widgetLayoutProvider;
    private readonly ConverterViewModel viewModel;
    private bool isSelfSaving;

    public Converter() : this(new ConverterModel())
    {
    }

    public Converter(ConverterModel model)
    {
        viewModel = new ConverterViewModel(model);
        InitializeWidget();
    }

    public Converter(IWidgetLayoutProvider widgetLayoutProvider, ConverterModel? model = null)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        var initialModel = model ?? widgetLayoutProvider.Get()?.GetModel<ConverterModel>() ?? new ConverterModel();
        viewModel = new ConverterViewModel(initialModel, SaveModelSelection);
        InitializeWidget();
    }

    private void InitializeWidget()
    {
        DataContext = viewModel;
        Unloaded += OnUnloaded;
        InitializeComponent();
    }

    public bool OnLayoutSettingsChanged(WidgetLayout newLayout)
    {
        if (isSelfSaving) return true;
        var updated = newLayout.GetModel<ConverterModel>();
        if (updated == null) return false;
        viewModel.ApplyModel(updated);
        return true;
    }

    private void SaveModelSelection(ConverterModel updated)
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

    private async void CopyCurrencyResult_OnClick(object? sender, RoutedEventArgs e) =>
        await viewModel.CopyCurrencyResultAsync(TopLevel.GetTopLevel(this));

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        Dispose();
    }

    public void Dispose()
    {
        viewModel.Dispose();
    }
}
