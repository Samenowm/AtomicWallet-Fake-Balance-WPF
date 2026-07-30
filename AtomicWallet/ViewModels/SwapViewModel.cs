using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Swap screen: functional intra-wallet exchange between two assets.</summary>
public sealed partial class SwapViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;

    /// <summary>0 = Instant Swap, 1 = Order History.</summary>
    [ObservableProperty]
    private int _selectedTab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToAmount), nameof(RateText), nameof(CanSwap), nameof(BelowMinimum), nameof(AvailableText))]
    [NotifyCanExecuteChangedFor(nameof(SwapCommand))]
    private Asset? _fromCoin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToAmount), nameof(RateText), nameof(CanSwap))]
    [NotifyCanExecuteChangedFor(nameof(SwapCommand))]
    private Asset? _toCoin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToAmount), nameof(CanSwap), nameof(BelowMinimum))]
    [NotifyCanExecuteChangedFor(nameof(SwapCommand))]
    private string _fromAmountText = "0.00";

    public SwapViewModel(MarketDataService market, NavigationService navigation,
        NotificationService notifications, Asset? fromCoin = null)
    {
        _market = market;
        _navigation = navigation;
        _notifications = notifications;
        Coins = market.Assets;
        FromCoin = fromCoin
                   ?? market.Assets.FirstOrDefault(a => a.Balance > 0)
                   ?? market.Assets.FirstOrDefault(a => a.Ticker == "ETH");
        ToCoin = market.Assets.FirstOrDefault(a => a.Id == "btc" && a != FromCoin)
                 ?? market.Assets.FirstOrDefault(a => a != FromCoin);
    }

    public ObservableCollection<Asset> Coins { get; }

    public decimal FromAmount =>
        decimal.TryParse(FromAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    /// <summary>Balance rounded to the 6 decimals the UI shows, so "Send all" / typing the
    /// shown amount isn't rejected for exceeding the (longer) true balance by a rounding dust.</summary>
    private decimal RoundedFromBalance =>
        FromCoin != null ? System.Math.Round(FromCoin.Balance, 6, System.MidpointRounding.AwayFromZero) : 0m;

    /// <summary>Amount actually swapped — never more than the real balance (avoids over-spend).</summary>
    private decimal EffectiveAmount =>
        FromCoin != null ? System.Math.Min(FromAmount, FromCoin.Balance) : 0m;

    public decimal ToAmount =>
        FromCoin != null && ToCoin != null && ToCoin.Price > 0
            ? EffectiveAmount * FromCoin.Price / ToCoin.Price
            : 0m;

    public string RateText =>
        FromCoin != null && ToCoin != null && ToCoin.Price > 0
            ? $"1 {FromCoin.Ticker} ≈ {FromCoin.Price / ToCoin.Price:0.######} {ToCoin.Ticker}"
            : string.Empty;

    public string AvailableText => FromCoin != null ? $"Available: {FromCoin.Balance:0.######} {FromCoin.Ticker}" : string.Empty;

    /// <summary>A small demo minimum so the warning UI matches the reference.</summary>
    public decimal Minimum => 0.0001m;

    public bool BelowMinimum => FromAmount > 0 && FromAmount < Minimum;

    public bool CanSwap =>
        FromCoin != null && ToCoin != null && FromCoin != ToCoin
        && FromAmount >= Minimum && FromAmount <= RoundedFromBalance;

    [RelayCommand]
    private void SendAll()
    {
        if (FromCoin != null)
        {
            FromAmountText = FromCoin.Balance.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }

    [RelayCommand] private void ShowInstant() => SelectedTab = 0;

    [RelayCommand] private void ShowHistory() => SelectedTab = 1;

    [RelayCommand]
    private void Flip()
    {
        (FromCoin, ToCoin) = (ToCoin, FromCoin);
        FromAmountText = "0.00";
    }

    [RelayCommand(CanExecute = nameof(CanSwap))]
    private void Swap()
    {
        if (FromCoin == null || ToCoin == null)
        {
            return;
        }

        var fromT = FromCoin.Ticker;
        var toT = ToCoin.Ticker;
        var amount = EffectiveAmount;
        if (_market.Swap(FromCoin, ToCoin, amount))
        {
            _notifications.Show($"Swapped {amount:0.######} {fromT} to {toT}");
            _navigation.Navigate(new WalletViewModel(_market, _navigation, _notifications), resetRoot: true);
        }
    }
}
