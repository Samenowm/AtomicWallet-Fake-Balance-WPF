using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using AtomicWallet.Controls;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Portfolio overview: a donut of holdings plus a sortable holdings table.</summary>
public sealed partial class PortfolioViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;
    private readonly ObservableCollection<Asset> _holdings;

    [ObservableProperty]
    private string _sortColumn = "PortfolioPercent";

    [ObservableProperty]
    private bool _sortAscending;

    public PortfolioViewModel(MarketDataService market, NavigationService navigation, NotificationService notifications)
    {
        _market = market;
        _navigation = navigation;
        _notifications = notifications;

        _holdings = new ObservableCollection<Asset>(market.Assets.Where(a => a.Balance > 0));
        Holdings = CollectionViewSource.GetDefaultView(_holdings);

        Segments = _holdings.Select(a => new DonutSegment
        {
            Value = (double)a.Value,
            Color = ToBrush(a.IconColor)
        }).ToList();

        ApplySort();
    }

    public ICollectionView Holdings { get; }

    public IReadOnlyList<DonutSegment> Segments { get; }

    public decimal TotalValue => _market.TotalValue;

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
        Holdings.SortDescriptions.Clear();
        Holdings.SortDescriptions.Add(new SortDescription(SortColumn,
            SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        Holdings.Refresh();
    }

    [RelayCommand]
    private void OpenAsset(Asset? asset)
    {
        if (asset != null)
        {
            _navigation.Navigate(new AssetDetailViewModel(asset, _market, _navigation, _notifications));
        }
    }

    private static Brush ToBrush(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.Gray; }
    }
}
