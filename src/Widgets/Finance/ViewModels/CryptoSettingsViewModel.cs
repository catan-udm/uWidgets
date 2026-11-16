using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Finance.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Finance.ViewModels;

public class CryptoSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private CryptoModel model;

    public CryptoSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<CryptoModel>() ?? new CryptoModel();

        Coins = new ObservableCollection<string>(model.TrackedCoins);

        AddCoinCommand = ReactiveCommand.Create(() =>
        {
            if (!string.IsNullOrWhiteSpace(NewCoinId))
            {
                var clean = NewCoinId.Trim().ToLowerInvariant();
                if (!Coins.Contains(clean))
                {
                    Coins.Add(clean);
                    NewCoinId = "";
                    Save();
                }
            }
        });

        RemoveCoinCommand = ReactiveCommand.Create<string>(coin =>
        {
            if (Coins.Remove(coin))
            {
                Save();
            }
        });
    }

    public ObservableCollection<string> Coins { get; }

    public IReadOnlyList<string> AvailableFiats { get; } =
    [
        "usd", "eur", "gbp", "jpy", "cad", "aud", "chf", "cny", "php", "sgd", "inr"
    ];

    public string FiatCurrency
    {
        get => model.FiatCurrency;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var lower = value.Trim().ToLowerInvariant();
            if (string.Equals(model.FiatCurrency, lower, StringComparison.OrdinalIgnoreCase)) return;
            model = model with { FiatCurrency = lower };
            Save();
            this.RaisePropertyChanged(nameof(FiatCurrency));
        }
    }

    public int RefreshMinutes
    {
        get => Math.Clamp(model.RefreshMinutes, 1, 60);
        set
        {
            var clamped = Math.Clamp(value, 1, 60);
            if (model.RefreshMinutes == clamped) return;
            model = model with { RefreshMinutes = clamped };
            Save();
            this.RaisePropertyChanged(nameof(RefreshMinutes));
        }
    }

    private string newCoinId = "";
    public string NewCoinId
    {
        get => newCoinId;
        set => this.RaiseAndSetIfChanged(ref newCoinId, value);
    }

    public ICommand AddCoinCommand { get; }
    public ICommand RemoveCoinCommand { get; }

    private void Save()
    {
        if (widgetLayoutProvider == null) return;
        model = model with { TrackedCoins = Coins.ToList() };
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
