using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
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
    public string SummaryTooltip => $"{Symbol} · {DisplayName}\n{FormattedPrice} ({FormattedDelta}, {FormattedChange})";

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
        this.RaisePropertyChanged(nameof(SummaryTooltip));
        this.RaisePropertyChanged(nameof(DeltaBrush));
    }
}

public class StocksViewModel : ReactiveObject, IDisposable
{
    private readonly StocksModel model;
    private readonly int refreshMinutes;
    private int elapsedMinutes;
    private int isUpdating;
    private bool isOffline;
    private bool isRefreshing;

    public StocksViewModel(StocksModel model)
    {
        this.model = model;
        refreshMinutes = Math.Clamp(model.RefreshMinutes, 1, 60);
        Quotes = [];

        RefreshCommand = ReactiveCommand.CreateFromTask(() => UpdateAsync(), outputScheduler: RxApp.MainThreadScheduler);

        _ = UpdateAsync();
        TimerService.Timer1Minute.Subscribe(OnMinuteTick);
    }

    public ObservableCollection<StockItemViewModel> Quotes { get; }
    public ICommand RefreshCommand { get; }
    public bool HasQuotes => Quotes.Count > 0;
    public bool IsEmpty => !HasQuotes;

    public bool IsOffline
    {
        get => isOffline;
        private set => this.RaiseAndSetIfChanged(ref isOffline, value);
    }

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set => this.RaiseAndSetIfChanged(ref isRefreshing, value);
    }

    private void OnMinuteTick()
    {
        elapsedMinutes++;
        if (elapsedMinutes < refreshMinutes) return;
        elapsedMinutes = 0;
        _ = UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        if (Interlocked.CompareExchange(ref isUpdating, 1, 0) != 0)
            return;

        await Dispatcher.UIThread.InvokeAsync(() => IsRefreshing = true);
        try
        {
            var result = await StockPriceService.GetStockQuotesWithStatusAsync(model.TrackedTickers);
            var list = result.Quotes;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsOffline = result.IsOfflineFallback;
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

                this.RaisePropertyChanged(nameof(HasQuotes));
                this.RaisePropertyChanged(nameof(IsEmpty));
            });
        }
        catch (Exception)
        {
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsRefreshing = false);
            Volatile.Write(ref isUpdating, 0);
        }
    }

    public void Dispose()
    {
        TimerService.Timer1Minute.Unsubscribe(OnMinuteTick);
        GC.SuppressFinalize(this);
    }
}
