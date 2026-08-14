using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Buy crypto: simulated purchase that credits the wallet balance.</summary>
public sealed partial class BuyCryptoViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;

    /// <summary>0 = Buy crypto, 1 = Order History.</summary>
    [ObservableProperty]
    private int _selectedTab;

    /// <summary>Fiat (USD) amount — editable; kept in sync with the crypto amount.</summary>
    [ObservableProperty]
    private string _amountUsdText = "200";

    /// <summary>Crypto amount — editable; kept in sync with the fiat amount.</summary>
    [ObservableProperty]
    private string _amountCryptoText = string.Empty;

    [ObservableProperty]
    private Asset? _toCoin;

    /// <summary>Guards the two-way fiat ⇄ crypto sync against re-entrancy.</summary>
    private bool _syncing;

    public BuyCryptoViewModel(MarketDataService market, NavigationService navigation,
        NotificationService notifications, Asset? coin = null)
    {
        _market = market;
        _navigation = navigation;
        _notifications = notifications;
        Coins = market.Assets;
        ToCoin = coin ?? market.Assets.FirstOrDefault(a => a.Id == "btc");
    }

    public ObservableCollection<Asset> Coins { get; }

    /// <summary>Card-purchase history shown in the Order History tab.</summary>
    public ObservableCollection<BuyOrder> Orders => _market.BuyOrders;

    public string FiatCode => "USD";

    public decimal AmountUsd =>
        decimal.TryParse(AmountUsdText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    public decimal AmountCrypto =>
        decimal.TryParse(AmountCryptoText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    public string RewardText => $"{AmountUsd * 0.0268m:0.########} AWC";

    private bool CanContinue() => ToCoin != null && AmountUsd >= 10m;

    // ===== Two-way fiat ⇄ crypto conversion =====

    partial void OnAmountUsdTextChanged(string value)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        AmountCryptoText = ToCoin is { Price: > 0 }
            ? (AmountUsd / ToCoin.Price).ToString("0.########", CultureInfo.InvariantCulture)
            : "0";
        _syncing = false;
        AfterAmountChanged();
    }

    partial void OnAmountCryptoTextChanged(string value)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        AmountUsdText = ToCoin is { Price: > 0 }
            ? (AmountCrypto * ToCoin.Price).ToString("0.##", CultureInfo.InvariantCulture)
            : "0";
        _syncing = false;
        AfterAmountChanged();
    }

    partial void OnToCoinChanged(Asset? value)
    {
        // Keep the fiat amount fixed and recompute the crypto side for the new coin.
        _syncing = true;
        AmountCryptoText = value is { Price: > 0 }
            ? (AmountUsd / value.Price).ToString("0.########", CultureInfo.InvariantCulture)
            : "0";
        _syncing = false;
        AfterAmountChanged();
    }

    private void AfterAmountChanged()
    {
        OnPropertyChanged(nameof(RewardText));
        ContinueCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand] private void ShowBuy() => SelectedTab = 0;

    [RelayCommand] private void ShowHistory() => SelectedTab = 1;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        if (ToCoin == null)
        {
            return;
        }

        var amount = ToCoin.Price > 0 ? AmountUsd / ToCoin.Price : 0m;
        var ticker = ToCoin.Ticker;
        _market.Buy(ToCoin, AmountUsd, amount);
        _notifications.Show($"Bought {amount:0.######} {ticker}");
        SelectedTab = 1;
    }
}
