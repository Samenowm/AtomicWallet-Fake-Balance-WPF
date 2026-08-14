using System.Collections.ObjectModel;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Detail screen for a single asset (Transactions / Price Chart / About).</summary>
public sealed partial class AssetDetailViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;

    /// <summary>0 = Transactions, 1 = Price Chart, 2 = About.</summary>
    [ObservableProperty]
    private int _selectedTab;

    /// <summary>Selected price-chart range: 24H / 1W / 1M / 1Y / ALL.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RangeLabel))]
    private string _selectedRange = "1W";

    /// <summary>Synthetic price series for the selected range (new instance per change so the chart redraws).</summary>
    [ObservableProperty]
    private System.Collections.Generic.IReadOnlyList<decimal> _chartSeries = System.Array.Empty<decimal>();

    /// <summary>Percentage move across the selected range (last vs first point).</summary>
    [ObservableProperty]
    private decimal _rangeChange;

    /// <summary>Guards against stale async chart responses when the range/coin changes.</summary>
    private int _chartToken;

    public AssetDetailViewModel(Asset coin, MarketDataService market, NavigationService navigation,
        NotificationService notifications)
    {
        Coin = coin;
        _market = market;
        _navigation = navigation;
        _notifications = notifications;
        _selectedSwitch = coin;

        Transactions = new System.Collections.ObjectModel.ObservableCollection<Models.TxItem>(
            System.Linq.Enumerable.Where(market.Transactions, t => t.Ticker == coin.Ticker));

        // Placeholder "About" cards (our own content — not the original articles).
        AboutCards = new ObservableCollection<string>
        {
            $"What is {coin.Name}?",
            $"How to buy {coin.Ticker}",
            $"{coin.Name} explained",
            $"Sending and receiving {coin.Ticker}",
            $"{coin.Name} network fees",
            $"Staking {coin.Ticker}",
            $"Is {coin.Name} secure?",
            $"{coin.Name} price history",
        };

        RebuildChart();
    }

    /// <summary>Short label for the selected range, shown next to the range change %.</summary>
    public string RangeLabel => SelectedRange;

    public Asset Coin { get; }

    /// <summary>All assets, for the in-header coin switcher.</summary>
    public System.Collections.ObjectModel.ObservableCollection<Asset> Coins => _market.Assets;

    [ObservableProperty]
    private Asset? _selectedSwitch;

    partial void OnSelectedSwitchChanged(Asset? value)
    {
        if (value != null && value != Coin)
        {
            _navigation.Navigate(new AssetDetailViewModel(value, _market, _navigation, _notifications));
        }
    }

    public System.Collections.ObjectModel.ObservableCollection<Models.TxItem> Transactions { get; }

    public bool HasTransactions => Transactions.Count > 0;

    public ObservableCollection<string> AboutCards { get; }

    public string[] Ranges { get; } = { "24H", "1W", "1M", "1Y", "ALL" };

    [RelayCommand]
    private void ShowTab(string index) => SelectedTab = int.Parse(index);

    [RelayCommand]
    private void SelectRange(string range) => SelectedRange = range;

    partial void OnSelectedRangeChanged(string value) => RebuildChart();

    /// <summary>
    /// Builds a deterministic, TradingView-style price series for the current range using a
    /// geometric random walk (many small steps), so the line looks soft and natural rather
    /// than a few sharp zig-zags. The seed is derived from coin + range, so each range has a
    /// stable shape that ends exactly on the live price. A new list instance is assigned so
    /// the AreaChart re-renders.
    /// </summary>
    private void RebuildChart()
    {
        var (points, stepVolatility, driftMagnitude) = SelectedRange switch
        {
            "24H" => (110, 0.0035, 0.020),
            "1W" => (140, 0.0050, 0.050),
            "1M" => (170, 0.0060, 0.120),
            "1Y" => (210, 0.0080, 0.500),
            "ALL" => (240, 0.0100, 1.200),
            _ => (140, 0.0050, 0.050)
        };

        var price = Coin.Price > 0 ? Coin.Price : 1m;
        var rng = new System.Random(System.HashCode.Combine(Coin.Id, SelectedRange));
        var direction = rng.NextDouble() < 0.5 ? -1.0 : 1.0;
        var driftPerStep = direction * driftMagnitude / points;

        // Geometric random walk: each step is a small multiplicative return.
        var path = new double[points];
        path[0] = 1.0;
        for (var i = 1; i < points; i++)
        {
            var ret = driftPerStep + (NextGaussian(rng) * stepVolatility);
            path[i] = System.Math.Max(0.0001, path[i - 1] * (1 + ret));
        }

        // Rescale the whole path so the final point lands on the live price.
        var scale = (double)price / path[points - 1];
        var series = new System.Collections.Generic.List<decimal>(points);
        foreach (var p in path)
        {
            series.Add((decimal)(p * scale));
        }

        ChartSeries = series;
        RangeChange = series[0] > 0
            ? System.Math.Round((price - series[0]) / series[0] * 100m, 2)
            : 0m;

        // Then replace the synthetic series with real historical prices when they arrive.
        var token = ++_chartToken;
        _ = LoadRealChartAsync(SelectedRange, token);
    }

    /// <summary>Fetches real price history for the range and swaps it in if still current.</summary>
    private async System.Threading.Tasks.Task LoadRealChartAsync(string range, int token)
    {
        var feed = Services.PriceFeedService.Instance;
        if (feed == null)
        {
            return;
        }

        var data = await feed.GetHistoryAsync(Coin.Id, range);
        if (data is not { Count: > 1 } || token != _chartToken)
        {
            return;
        }

        ChartSeries = data;
        RangeChange = data[0] > 0
            ? System.Math.Round((data[^1] - data[0]) / data[0] * 100m, 2)
            : 0m;
    }

    /// <summary>Standard normal sample (Box–Muller) for the price walk.</summary>
    private static double NextGaussian(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        return System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2);
    }

    [RelayCommand]
    private void Receive() => _navigation.Navigate(new ReceiveViewModel(Coin, _navigation, _notifications));

    [RelayCommand]
    private void Send() => _navigation.Navigate(new SendViewModel(Coin, _navigation, _market, _notifications));

    [RelayCommand]
    private void Swap() => _navigation.Navigate(new SwapViewModel(_market, _navigation, _notifications, Coin));

    [RelayCommand]
    private void Buy() => _navigation.Navigate(new BuyCryptoViewModel(_market, _navigation, _notifications, Coin));

    [RelayCommand]
    private void Close() => _navigation.GoBack();
}
