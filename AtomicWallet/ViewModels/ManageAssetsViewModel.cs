using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>
/// Manage Assets: choose which coins/tokens appear in the wallet. Changes are staged
/// on the chips and committed to the assets (and persisted) only on Apply.
/// </summary>
public sealed partial class ManageAssetsViewModel : ViewModelBase
{
    private readonly MarketDataService _market;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;
    private bool _suppressCascade;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _coinsEnabled = true;

    [ObservableProperty]
    private bool _tokensEnabled = true;

    [ObservableProperty]
    private bool _hideZeroBalance;

    [ObservableProperty]
    private bool _notificationsDisabled;

    public ManageAssetsViewModel(MarketDataService market, NavigationService navigation,
        NotificationService notifications)
    {
        _market = market;
        _navigation = navigation;
        _notifications = notifications;

        Coins = BuildChips(token: false);
        Tokens = BuildChips(token: true);

        CoinsView = CollectionViewSource.GetDefaultView(Coins);
        CoinsView.Filter = MatchesSearch;
        TokensView = CollectionViewSource.GetDefaultView(Tokens);
        TokensView.Filter = MatchesSearch;

        // Initialise toggle/group state from current data without cascading.
        _suppressCascade = true;
        _coinsEnabled = Coins.Any(c => c.IsEnabled);
        _tokensEnabled = Tokens.Any(c => c.IsEnabled);
        _hideZeroBalance = market.HideZeroBalance;
        _notificationsDisabled = !notifications.Enabled;
        _suppressCascade = false;
    }

    public ObservableCollection<AssetChip> Coins { get; }

    public ObservableCollection<AssetChip> Tokens { get; }

    public ICollectionView CoinsView { get; }

    public ICollectionView TokensView { get; }

    [RelayCommand]
    private void Apply()
    {
        foreach (var chip in Coins.Concat(Tokens))
        {
            if (chip.Asset == null)
            {
                continue;
            }

            // Funded assets are always kept visible.
            chip.Asset.Visible = chip.IsEnabled || chip.Asset.Balance > 0;
        }

        _market.HideZeroBalance = HideZeroBalance;
        _market.NotificationsEnabled = !NotificationsDisabled;
        _notifications.Enabled = !NotificationsDisabled;
        _market.Save();

        // The wallet shares this default view; refreshing re-applies its filter.
        CollectionViewSource.GetDefaultView(_market.Assets).Refresh();

        _navigation.GoBack();
        _notifications.Show("Asset list updated");
    }

    [RelayCommand]
    private void Cancel() => _navigation.GoBack();

    [RelayCommand]
    private void HideAll()
    {
        _suppressCascade = true;
        foreach (var c in Coins) c.IsEnabled = c.HasBalance;
        foreach (var t in Tokens) t.IsEnabled = t.HasBalance;
        CoinsEnabled = false;
        TokensEnabled = false;
        _suppressCascade = false;
    }

    partial void OnSearchTextChanged(string value)
    {
        CoinsView.Refresh();
        TokensView.Refresh();
    }

    partial void OnCoinsEnabledChanged(bool value)
    {
        if (_suppressCascade)
        {
            return;
        }

        foreach (var c in Coins) c.IsEnabled = value || c.HasBalance;
    }

    partial void OnTokensEnabledChanged(bool value)
    {
        if (_suppressCascade)
        {
            return;
        }

        foreach (var t in Tokens) t.IsEnabled = value || t.HasBalance;
    }

    private bool MatchesSearch(object obj)
    {
        if (obj is not AssetChip chip)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(SearchText)
               || chip.Ticker.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase);
    }

    private ObservableCollection<AssetChip> BuildChips(bool token)
    {
        var list = new ObservableCollection<AssetChip>();
        foreach (var entry in _market.Catalog.Where(c => c.IsToken == token))
        {
            var asset = _market.Assets.FirstOrDefault(a => a.Id == entry.Id);
            if (asset == null)
            {
                continue;
            }

            list.Add(new AssetChip
            {
                Asset = asset,
                Ticker = entry.Ticker,
                IconColor = entry.Color,
                LogoKey = asset.LogoKey,
                IsEnabled = asset.Visible
            });
        }

        return list;
    }
}
