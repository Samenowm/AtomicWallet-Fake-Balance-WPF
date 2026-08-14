using System.Globalization;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Send screen: functional debit of the demo balance.</summary>
public sealed partial class SendViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;
    private bool _syncing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _addressText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _amountText = "0.00";

    [ObservableProperty]
    private string _amountFiatText = "0.00";

    [ObservableProperty]
    private bool _feeOpen;

    [ObservableProperty]
    private double _feePerByte = 5;

    public SendViewModel(Asset coin, NavigationService navigation, MarketDataService market,
        NotificationService notifications)
    {
        Coin = coin;
        _navigation = navigation;
        _market = market;
        _notifications = notifications;
    }

    public Asset Coin { get; }

    public decimal Amount =>
        decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    /// <summary>Balance rounded to the 6 decimals the UI shows, so "Send all" / typing the
    /// shown amount isn't rejected for exceeding the (longer) true balance by a rounding dust.</summary>
    private decimal RoundedBalance =>
        System.Math.Round(Coin.Balance, 6, System.MidpointRounding.AwayFromZero);

    /// <summary>Amount actually sent — never more than the real balance (avoids over-spend).</summary>
    private decimal EffectiveAmount => System.Math.Min(Amount, Coin.Balance);

    private bool CanSubmit() =>
        !string.IsNullOrWhiteSpace(AddressText) && Amount > 0 && Amount <= RoundedBalance;

    partial void OnAmountTextChanged(string value)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        AmountFiatText = (Amount * Coin.Price).ToString("0.00", CultureInfo.InvariantCulture);
        _syncing = false;
    }

    partial void OnAmountFiatTextChanged(string value)
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        var fiat = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var f) ? f : 0m;
        AmountText = (Coin.Price > 0 ? fiat / Coin.Price : 0m).ToString("0.######", CultureInfo.InvariantCulture);
        _syncing = false;
    }

    [RelayCommand]
    private void SendAll()
    {
        AmountText = Coin.Balance.ToString("0.######", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void ToggleFee() => FeeOpen = !FeeOpen;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        var amount = EffectiveAmount;
        var ticker = Coin.Ticker;
        if (_market.Send(Coin, AddressText.Trim(), amount))
        {
            _notifications.Show($"Sent {amount:0.######} {ticker}");
            _navigation.Navigate(new WalletViewModel(_market, _navigation, _notifications), resetRoot: true);
        }
    }

    [RelayCommand]
    private void Close() => _navigation.GoBack();
}
