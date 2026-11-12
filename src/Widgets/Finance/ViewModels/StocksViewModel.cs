using System.Collections.ObjectModel;
using Avalonia;
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

    private static readonly IBrush PosLight = new SolidColorBrush(Color.Parse("#047857"));
    private static readonly IBrush PosDark = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush NegLight = new SolidColorBrush(Color.Parse("#B91C1C"));
    private static readonly IBrush NegDark = new SolidColorBrush(Color.Parse("#F87171"));

    public IBrush DeltaBrush
    {
        get
        {
            var isDark = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;
            return IsPositive ? (isDark ? PosDark : PosLight) : (isDark ? NegDark : NegLight);
        }
    }

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
