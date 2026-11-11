using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using Finance.Models;
using Finance.Services;
using ReactiveUI;
using uWidgets.Services;

namespace Finance.ViewModels;

public class StockItemViewModel : ReactiveObject
{
    private StockQuoteInfo info;

    public StockItemViewModel(StockQuoteInfo info)
    {
        this.info = info;
    }

    public string Symbol => info.Symbol;
    public string DisplayName => info.DisplayName;
    public string FormattedPrice => info.FormattedPrice;
    public string FormattedChange => info.FormattedChange;
    public string FormattedDelta => info.FormattedDelta;
    public bool IsPositive => info.IsPositive;

    public IBrush DeltaBrush => IsPositive
        ? new SolidColorBrush(Color.Parse("#10B981"))
        : new SolidColorBrush(Color.Parse("#EF4444"));

    public void Update(StockQuoteInfo newInfo)
    {
        info = newInfo;
        this.RaisePropertyChanged(nameof(DisplayName));
        this.RaisePropertyChanged(nameof(FormattedPrice));
        this.RaisePropertyChanged(nameof(FormattedChange));
        this.RaisePropertyChanged(nameof(FormattedDelta));
        this.RaisePropertyChanged(nameof(IsPositive));
        this.RaisePropertyChanged(nameof(DeltaBrush));
    }
}

public class StocksViewModel : ReactiveObject, IDisposable
{
    private readonly StocksModel model;

    public StocksViewModel(StocksModel model)
    {
        this.model = model;
        Quotes = [];

        Update();
        TimerService.Timer1Minute.Subscribe(Update);
    }

    public ObservableCollection<StockItemViewModel> Quotes { get; }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        try
        {
            var list = await StockPriceService.GetStockQuotesAsync(model.TrackedTickers);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var toRemove = Quotes.Where(q => list.All(l => l.Symbol != q.Symbol)).ToList();
                foreach (var rem in toRemove) Quotes.Remove(rem);

                foreach (var item in list)
                {
                    var existing = Quotes.FirstOrDefault(q => q.Symbol == item.Symbol);
                    if (existing != null)
                    {
                        existing.Update(item);
                    }
                    else
                    {
                        Quotes.Add(new StockItemViewModel(item));
                    }
                }
            });
        }
        catch (Exception)
        {
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(Update);
        GC.SuppressFinalize(this);
    }
}
