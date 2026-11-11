using System.Windows.Input;
using Finance.Models;
using Finance.Services;
using ReactiveUI;

namespace Finance.ViewModels;

public class ConverterViewModel : ReactiveObject
{
    private readonly ConverterModel model;
    private Dictionary<string, double> exchangeRates = new();

    public ConverterViewModel(ConverterModel model)
    {
        this.model = model;

        SwapCurrenciesCommand = ReactiveCommand.Create(() =>
        {
            (SourceCurrency, TargetCurrency) = (TargetCurrency, SourceCurrency);
            Calculate();
        });

        LoadRatesAsync();
    }

    private async void LoadRatesAsync()
    {
        exchangeRates = await CurrencyExchangeService.GetExchangeRatesAsync();
        Calculate();
    }

    public List<string> Currencies { get; } =
    [
        "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "PHP", "SGD", "INR"
    ];

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

    private string sourceCurrency = "USD";
    public string SourceCurrency
    {
        get => sourceCurrency;
        set
        {
            this.RaiseAndSetIfChanged(ref sourceCurrency, value);
            Calculate();
        }
    }

    private string targetCurrency = "EUR";
    public string TargetCurrency
    {
        get => targetCurrency;
        set
        {
            this.RaiseAndSetIfChanged(ref targetCurrency, value);
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
            RemValue = UnitConverterService.PxToRem(value);
        }
    }

    private double remValue = 1.0;
    public double RemValue
    {
        get => remValue;
        set
        {
            this.RaiseAndSetIfChanged(ref remValue, value);
            this.RaisePropertyChanged(nameof(RemValue));
        }
    }

    public ICommand SwapCurrenciesCommand { get; }

    private void Calculate()
    {
        if (exchangeRates.Count == 0) return;
        double res = CurrencyExchangeService.Convert(SourceAmount, SourceCurrency, TargetCurrency, exchangeRates);
        ConvertedResult = $"{res:N2} {TargetCurrency}";
    }
}
