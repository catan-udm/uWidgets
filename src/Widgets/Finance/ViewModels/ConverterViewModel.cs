using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using Finance.Models;
using Finance.Services;
using ReactiveUI;

namespace Finance.ViewModels;

public class ConverterViewModel : ReactiveObject, IDisposable
{
    private ConverterModel model;
    private readonly Action<ConverterModel>? onModelUpdated;
    private Dictionary<string, double> exchangeRates = new();
    private bool isSyncingCss;

    public ConverterViewModel(ConverterModel model, Action<ConverterModel>? onModelUpdated = null)
    {
        this.model = model;
        this.onModelUpdated = onModelUpdated;

        sourceCurrency = string.IsNullOrWhiteSpace(model.SourceCurrency) ? "USD" : model.SourceCurrency.ToUpperInvariant();
        targetCurrency = string.IsNullOrWhiteSpace(model.TargetCurrency) ? "EUR" : model.TargetCurrency.ToUpperInvariant();
        EnsureCurrencyOption(sourceCurrency);
        EnsureCurrencyOption(targetCurrency);

        pxValue = Math.Clamp(model.RootFontSizePx, 8.0, 64.0);
        remValue = UnitConverterService.PxToRem(pxValue, RootFontSizePx);
        CalculateStorage();

        SwapCurrenciesCommand = ReactiveCommand.Create(() =>
        {
            (SourceCurrency, TargetCurrency) = (TargetCurrency, SourceCurrency);
            Calculate();
        }, outputScheduler: RxApp.MainThreadScheduler);

        RefreshRatesCommand = ReactiveCommand.CreateFromTask(() => LoadRatesAsync(forceRefresh: true), outputScheduler: RxApp.MainThreadScheduler);

        _ = LoadRatesAsync(forceRefresh: false);
    }

    private double RootFontSizePx => Math.Clamp(model.RootFontSizePx, 8.0, 64.0);
    public string BaseFontSizeBadge => $"Base: {RootFontSizePx:0.#}px";

    public void ApplyModel(ConverterModel updated)
    {
        model = updated;
        EnsureCurrencyOption(updated.SourceCurrency);
        EnsureCurrencyOption(updated.TargetCurrency);

        isSyncingCss = true;
        try
        {
            remValue = UnitConverterService.PxToRem(pxValue, RootFontSizePx);
            this.RaisePropertyChanged(nameof(RemValue));
        }
        finally
        {
            isSyncingCss = false;
        }

        this.RaisePropertyChanged(nameof(BaseFontSizeBadge));
        Calculate();
    }

    private void EnsureCurrencyOption(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var upper = code.ToUpperInvariant();
            if (!Currencies.Contains(upper))
                Currencies.Add(upper);
        }
    }

    private async Task LoadRatesAsync(bool forceRefresh)
    {
        if (IsRefreshing) return;
        IsRefreshing = true;
        try
        {
            var result = await CurrencyExchangeService.GetExchangeRatesWithStatusAsync(forceRefresh);
            exchangeRates = result.Rates;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var key in exchangeRates.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    if (!Currencies.Contains(key))
                        Currencies.Add(key);
                }

                RatesStatus = result.IsOfflineFallback ? "Offline rates" : "";
                Calculate();
            });
        }
        catch (Exception)
        {
            await Dispatcher.UIThread.InvokeAsync(() => RatesStatus = "Offline rates");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsRefreshing = false);
        }
    }

    public ObservableCollection<string> Currencies { get; } =
    [
        "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "PHP", "SGD", "INR"
    ];

    public IReadOnlyList<string> StorageUnits { get; } = ["B", "KB", "MB", "GB", "TB"];

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        private set => this.RaiseAndSetIfChanged(ref isRefreshing, value);
    }

    private string ratesStatus = "";
    public string RatesStatus
    {
        get => ratesStatus;
        private set => this.RaiseAndSetIfChanged(ref ratesStatus, value);
    }

    private double sourceAmount = 100.0;
    public double SourceAmount
    {
        get => sourceAmount;
        set
        {
            this.RaiseAndSetIfChanged(ref sourceAmount, value);
            Calculate();
        }
    }

    private string sourceCurrency;
    public string SourceCurrency
    {
        get => sourceCurrency;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (this.RaiseAndSetIfChanged(ref sourceCurrency, value) == value && !string.Equals(model.SourceCurrency, value, StringComparison.OrdinalIgnoreCase))
            {
                model = model with { SourceCurrency = value };
                onModelUpdated?.Invoke(model);
            }
            Calculate();
        }
    }

    private string targetCurrency;
    public string TargetCurrency
    {
        get => targetCurrency;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (this.RaiseAndSetIfChanged(ref targetCurrency, value) == value && !string.Equals(model.TargetCurrency, value, StringComparison.OrdinalIgnoreCase))
            {
                model = model with { TargetCurrency = value };
                onModelUpdated?.Invoke(model);
            }
            Calculate();
        }
    }

    private string convertedResult = "---";
    public string ConvertedResult
    {
        get => convertedResult;
        private set => this.RaiseAndSetIfChanged(ref convertedResult, value);
    }

    private double pxValue = 16.0;
    public double PxValue
    {
        get => pxValue;
        set
        {
            this.RaiseAndSetIfChanged(ref pxValue, value);
            if (isSyncingCss) return;

            isSyncingCss = true;
            try
            {
                RemValue = UnitConverterService.PxToRem(value, RootFontSizePx);
            }
            finally
            {
                isSyncingCss = false;
            }
        }
    }

    private double remValue = 1.0;
    public double RemValue
    {
        get => remValue;
        set
        {
            this.RaiseAndSetIfChanged(ref remValue, value);
            if (isSyncingCss) return;

            isSyncingCss = true;
            try
            {
                PxValue = UnitConverterService.RemToPx(value, RootFontSizePx);
            }
            finally
            {
                isSyncingCss = false;
            }
        }
    }

    private double storageAmount = 1024.0;
    public double StorageAmount
    {
        get => storageAmount;
        set
        {
            this.RaiseAndSetIfChanged(ref storageAmount, value);
            CalculateStorage();
        }
    }

    private string fromStorageUnit = "MB";
    public string FromStorageUnit
    {
        get => fromStorageUnit;
        set
        {
            this.RaiseAndSetIfChanged(ref fromStorageUnit, value);
            CalculateStorage();
        }
    }

    private string toStorageUnit = "GB";
    public string ToStorageUnit
    {
        get => toStorageUnit;
        set
        {
            this.RaiseAndSetIfChanged(ref toStorageUnit, value);
            CalculateStorage();
        }
    }

    private string storageResult = "1 GB";
    public string StorageResult
    {
        get => storageResult;
        private set => this.RaiseAndSetIfChanged(ref storageResult, value);
    }

    public ICommand SwapCurrenciesCommand { get; }
    public ICommand RefreshRatesCommand { get; }

    public async Task CopyCurrencyResultAsync(TopLevel? topLevel)
    {
        if (topLevel?.Clipboard == null || string.IsNullOrWhiteSpace(ConvertedResult) || ConvertedResult == "---") return;
        await topLevel.Clipboard.SetTextAsync(ConvertedResult);
    }

    private void Calculate()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (exchangeRates.Count == 0) return;
            double res = CurrencyExchangeService.Convert(SourceAmount, SourceCurrency, TargetCurrency, exchangeRates);
            ConvertedResult = $"{res:N2} {TargetCurrency}";
        });
    }

    private void CalculateStorage()
    {
        var res = UnitConverterService.ConvertStorageBytes(StorageAmount, FromStorageUnit, ToStorageUnit);
        StorageResult = $"{res:G6} {ToStorageUnit}";
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
