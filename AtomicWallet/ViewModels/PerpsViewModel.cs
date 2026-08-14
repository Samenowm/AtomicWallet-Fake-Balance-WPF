using System.Collections.ObjectModel;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Perps screen: Market / Positions / History tabs.</summary>
public sealed partial class PerpsViewModel : ViewModelBase
{
    /// <summary>0 = Market, 1 = Positions, 2 = History.</summary>
    [ObservableProperty]
    private int _selectedTab;

    private readonly NotificationService _notifications;

    public PerpsViewModel(MarketDataService market, NotificationService notifications)
    {
        Markets = market.PerpMarkets;
        _notifications = notifications;
    }

    public ObservableCollection<PerpMarket> Markets { get; }

    [RelayCommand] private void ShowMarket() => SelectedTab = 0;

    [RelayCommand] private void ShowPositions() => SelectedTab = 1;

    [RelayCommand] private void ShowHistory() => SelectedTab = 2;

    [RelayCommand] private void AddFunds() => _notifications.Show("Add funds to your trading balance via Buy crypto");

    [RelayCommand] private void Withdraw() => _notifications.Show("No trading balance to withdraw");
}
