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

public class CryptoItemViewModel : ReactiveObject
{
    private CryptoCoinInfo info;
    private bool showSparkline;

    public CryptoItemViewModel(CryptoCoinInfo info, bool showSparkline = true)
    {
        this.info = info;
        this.showSparkline = showSparkline;
        SparklineGeometry = CryptoPriceService.GenerateSparklineGeometry(info.SparklinePrices);
    }

    public string Id => info.Id;
    public string Symbol => info.Symbol;
    public string Name => info.Name;
    public string FormattedPrice => info.FormattedPrice;
    public string FormattedChange => info.FormattedChange;
    public bool IsPositive => info.IsPositive;
    public string SummaryTooltip => $"{Name} ({Symbol})\n{FormattedPrice} ({FormattedChange})";

    public bool ShowSparkline
    {
        get => showSparkline;
        set => this.RaiseAndSetIfChanged(ref showSparkline, value);
    }

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

    private StreamGeometry sparklineGeometry = new();
    public StreamGeometry SparklineGeometry
    {
        get => sparklineGeometry;
        private set => this.RaiseAndSetIfChanged(ref sparklineGeometry, value);
    }

    public void Update(CryptoCoinInfo newInfo)
    {
        info = newInfo;
        SparklineGeometry = CryptoPriceService.GenerateSparklineGeometry(newInfo.SparklinePrices);
        this.RaisePropertyChanged(nameof(FormattedPrice));
        this.RaisePropertyChanged(nameof(FormattedChange));
        this.RaisePropertyChanged(nameof(IsPositive));
        this.RaisePropertyChanged(nameof(SummaryTooltip));
        this.RaisePropertyChanged(nameof(DeltaBrush));
    }
}

public class CryptoViewModel : ReactiveObject, IDisposable
{
    private readonly CryptoModel model;
    private readonly int refreshMinutes;
    private int elapsedMinutes;
    private int isUpdating;
    private bool isCompact;
    private bool isOffline;
    private bool isRefreshing;

    public CryptoViewModel(CryptoModel model)
    {
        this.model = model;
        refreshMinutes = Math.Clamp(model.RefreshMinutes, 1, 60);
        Coins = [];

        RefreshCommand = ReactiveCommand.CreateFromTask(() => UpdateAsync(), outputScheduler: RxApp.MainThreadScheduler);

        _ = UpdateAsync();
        TimerService.Timer1Minute.Subscribe(OnMinuteTick);
    }

    public ObservableCollection<CryptoItemViewModel> Coins { get; }
    public ICommand RefreshCommand { get; }
    public bool HasCoins => Coins.Count > 0;
    public bool IsEmpty => !HasCoins;

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

    public bool IsCompact
    {
        get => isCompact;
        set
        {
            if (this.RaiseAndSetIfChanged(ref isCompact, value) == value)
            {
                foreach (var coin in Coins)
                    coin.ShowSparkline = !value;
            }
        }
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
            var result = await CryptoPriceService.GetCryptoPricesWithStatusAsync(model.TrackedCoins, model.FiatCurrency);
            var list = result.Coins;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsOffline = result.IsOfflineFallback;
                var toRemove = Coins.Where(c => list.All(l => l.Id != c.Id)).ToList();
                foreach (var rem in toRemove) Coins.Remove(rem);

                foreach (var item in list)
                {
                    var existing = Coins.FirstOrDefault(c => c.Id == item.Id);
                    if (existing != null)
                    {
                        existing.Update(item);
                    }
                    else
                    {
                        Coins.Add(new CryptoItemViewModel(item, !IsCompact));
                    }
                }

                this.RaisePropertyChanged(nameof(HasCoins));
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
