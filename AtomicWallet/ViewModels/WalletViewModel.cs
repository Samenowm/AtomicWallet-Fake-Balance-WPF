using System;
using System.ComponentModel;
using System.Windows.Data;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Wallet screen: the full asset table with search.</summary>
public sealed partial class WalletViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _sortColumn = "MarketCapB";

    [ObservableProperty]
    private bool _sortAscending;

    public WalletViewModel(MarketDataService market, NavigationService navigation, NotificationService notifications)
    {
        _market = market;
        _navigation = navigation;
        _notifications = notifications;
        Assets = CollectionViewSource.GetDefaultView(market.Assets);
        Assets.Filter = Matches;
        ApplySort();
    }

    public ICollectionView Assets { get; }

    [RelayCommand]
    private void Sort(string column)
    {
        if (SortColumn == column)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = column;
            SortAscending = false;
        }

        ApplySort();
    }

    private void ApplySort()
    {
        Assets.SortDescriptions.Clear();
        Assets.SortDescriptions.Add(new SortDescription(SortColumn,
            SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        Assets.Refresh();
    }

    [RelayCommand]
    private void OpenAsset(Asset? asset)
    {
        if (asset != null)
        {
            _navigation.Navigate(new AssetDetailViewModel(asset, _market, _navigation, _notifications));
        }
    }

    [RelayCommand]
    private void ManageAssets() =>
        _navigation.Navigate(new ManageAssetsViewModel(_market, _navigation, _notifications));

    [RelayCommand]
    private void Refresh()
    {
        ApplySort();
        _notifications.Show("Balances refreshed");
    }

    private bool Matches(object obj)
    {
        if (obj is not Asset a)
        {
            return false;
        }

        if (!a.Visible)
        {
            return false;
        }

        if (_market.HideZeroBalance && a.Balance <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
               || a.Ticker.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchTextChanged(string value) => Assets.Refresh();
}
