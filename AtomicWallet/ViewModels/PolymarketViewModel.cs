using System.Collections.ObjectModel;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Polymarket screen: Markets / Positions / History tabs + category chips.</summary>
public sealed partial class PolymarketViewModel : ViewModelBase
{
    /// <summary>0 = Markets, 1 = Positions, 2 = History.</summary>
    [ObservableProperty]
    private int _selectedTab;

    [ObservableProperty]
    private string _selectedCategory = "TRENDING";

    private readonly NotificationService _notifications;

    public PolymarketViewModel(MarketDataService market, NotificationService notifications)
    {
        Cards = market.PolyCards;
        _notifications = notifications;
    }

    public ObservableCollection<PolyCard> Cards { get; }

    public string[] Categories { get; } =
    {
        "TRENDING", "BREAKING", "NEW", "POLITICS", "SPORTS", "FINANCE",
        "CRYPTO", "GEOPOLITICS", "TECH", "CULTURE", "WORLD", "ECONOMY"
    };

    [RelayCommand] private void ShowMarkets() => SelectedTab = 0;

    [RelayCommand] private void ShowPositions() => SelectedTab = 1;

    [RelayCommand] private void ShowHistory() => SelectedTab = 2;

    [RelayCommand] private void SelectCategory(string category) => SelectedCategory = category;

    [RelayCommand] private void AddFunds() => _notifications.Show("Add funds to your Polymarket balance via Buy crypto");

    [RelayCommand] private void Withdraw() => _notifications.Show("No balance to withdraw");

    [RelayCommand] private void Bet() => _notifications.Show("Add funds to place a bet");
}
