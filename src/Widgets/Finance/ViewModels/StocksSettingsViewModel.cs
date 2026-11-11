using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Finance.Models;
using ReactiveUI;
using uWidgets.Core.Interfaces;

namespace Finance.ViewModels;

public class StocksSettingsViewModel : ReactiveObject
{
    private readonly IWidgetLayoutProvider widgetLayoutProvider;
    private StocksModel model;

    public StocksSettingsViewModel(IWidgetLayoutProvider widgetLayoutProvider)
    {
        this.widgetLayoutProvider = widgetLayoutProvider;
        model = widgetLayoutProvider?.Get()?.GetModel<StocksModel>() ?? new StocksModel();

        Tickers = new ObservableCollection<string>(model.TrackedTickers);

        AddTickerCommand = ReactiveCommand.Create(() =>
        {
            if (!string.IsNullOrWhiteSpace(NewTicker))
            {
                var clean = NewTicker.Trim().ToUpperInvariant();
                if (!Tickers.Contains(clean))
                {
                    Tickers.Add(clean);
                    NewTicker = "";
                    Save();
                }
            }
        });

        RemoveTickerCommand = ReactiveCommand.Create<string>(ticker =>
        {
            if (Tickers.Remove(ticker))
            {
                Save();
            }
        });
    }

    public ObservableCollection<string> Tickers { get; }

    private string newTicker = "";
    public string NewTicker
    {
        get => newTicker;
        set => this.RaiseAndSetIfChanged(ref newTicker, value);
    }

    public ICommand AddTickerCommand { get; }
    public ICommand RemoveTickerCommand { get; }

    private void Save()
    {
        if (widgetLayoutProvider == null) return;
        model = model with { TrackedTickers = Tickers.ToList() };
        var widgetSettings = widgetLayoutProvider.Get();
        widgetLayoutProvider.Save(widgetSettings with { Settings = JsonSerializer.SerializeToElement(model) });
    }
}
