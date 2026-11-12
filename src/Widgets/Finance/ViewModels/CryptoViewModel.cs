using System.Collections.ObjectModel;
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

    public CryptoItemViewModel(CryptoCoinInfo info)
    {
        this.info = info;
        SparklineGeometry = CryptoPriceService.GenerateSparklineGeometry(info.SparklinePrices);
    }

    public string Id => info.Id;
    public string Symbol => info.Symbol;
    public string Name => info.Name;
    public string FormattedPrice => info.FormattedPrice;
    public string FormattedChange => info.FormattedChange;
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
        this.RaisePropertyChanged(nameof(DeltaBrush));
    }
}

public class CryptoViewModel : ReactiveObject, IDisposable
{
    private readonly CryptoModel model;

    public CryptoViewModel(CryptoModel model)
    {
        this.model = model;
        Coins = [];

        Update();
        TimerService.Timer1Minute.Subscribe(Update);
    }

    public ObservableCollection<CryptoItemViewModel> Coins { get; }

    private void Update() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        try
        {
            var list = await CryptoPriceService.GetCryptoPricesAsync(model.TrackedCoins, model.FiatCurrency);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
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
                        Coins.Add(new CryptoItemViewModel(item));
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
