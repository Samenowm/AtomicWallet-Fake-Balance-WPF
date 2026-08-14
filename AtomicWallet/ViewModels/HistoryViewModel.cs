using System;
using System.ComponentModel;
using System.Windows.Data;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Global transaction history with search and expandable rows.</summary>
public sealed partial class HistoryViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    public HistoryViewModel(MarketDataService market)
    {
        Transactions = CollectionViewSource.GetDefaultView(market.Transactions);
        Transactions.Filter = Matches;
    }

    public ICollectionView Transactions { get; }

    private bool Matches(object obj)
    {
        if (obj is not TxItem t)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(SearchText)
               || t.Ticker.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
               || t.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchTextChanged(string value) => Transactions.Refresh();

    [RelayCommand]
    private void Toggle(TxItem? tx)
    {
        if (tx != null)
        {
            tx.IsExpanded = !tx.IsExpanded;
        }
    }

    [RelayCommand]
    private void Copy(string? text)
    {
        try { System.Windows.Clipboard.SetText(text ?? string.Empty); }
        catch { /* clipboard may be locked */ }
    }
}
