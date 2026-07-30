using System.Collections.ObjectModel;
using System.ComponentModel;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Root view model: sidebar nav, currency widget, content host, toasts.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedCurrency = "USD";

    [ObservableProperty]
    private bool _confirmExitVisible;

    [RelayCommand]
    private void ConfirmExit() => System.Windows.Application.Current.Shutdown();

    [RelayCommand]
    private void CancelExit() => ConfirmExitVisible = false;

    partial void OnSelectedCurrencyChanged(string value)
    {
        Services.Fx.Instance.Set(value);
        Market.Save();
    }

    public ShellViewModel(MarketDataService market, NavigationService navigation, NotificationService notifications)
    {
        Market = market;
        Navigation = navigation;
        Notifications = notifications;

        NavItems = new ObservableCollection<NavItem>
        {
            new("Wallet", "IconWallet"),
            new("Swap", "IconSwap"),
            new("Buy crypto", "IconBuy"),
            new("History", "IconHistory"),
            new("Perps", "IconPerps"),
            new("Polymarket", "IconPolymarket"),
            new("Staking", "IconStaking"),
            new("NFT gallery", "IconNft"),
            new("Settings", "IconSettings"),
            new("Support", "IconSupport"),
            new("Exit", "IconExit"),
        };

        Currencies = new ObservableCollection<string> { "USD", "EUR", "GBP", "JPY", "TRY", "BTC" };
        _selectedCurrency = Services.Fx.Instance.Code; // reflect any persisted currency

        Navigation.PropertyChanged += OnNavigationChanged;
        Navigate(NavItems[0]);
    }

    public MarketDataService Market { get; }

    public NavigationService Navigation { get; }

    public NotificationService Notifications { get; }

    public ObservableCollection<NavItem> NavItems { get; }

    public ObservableCollection<string> Currencies { get; }

    [RelayCommand]
    private void Navigate(NavItem item)
    {
        if (item.Title == "Exit")
        {
            ConfirmExitVisible = true;
            return;
        }

        ViewModelBase page = item.Title switch
        {
            "Wallet" => new WalletViewModel(Market, Navigation, Notifications),
            "Swap" => new SwapViewModel(Market, Navigation, Notifications),
            "Buy crypto" => new BuyCryptoViewModel(Market, Navigation, Notifications),
            "History" => new HistoryViewModel(Market),
            "Perps" => new PerpsViewModel(Market, Notifications),
            "Polymarket" => new PolymarketViewModel(Market, Notifications),
            "Staking" => new StakingViewModel(Market),
            "NFT gallery" => new NftGalleryViewModel(),
            "Settings" => new SettingsViewModel(Notifications),
            _ => new PlaceholderViewModel(item.Title)
        };

        Navigation.Navigate(page, resetRoot: true);
    }

    [RelayCommand]
    private void OpenPortfolio() => Navigation.Navigate(new PortfolioViewModel(Market, Navigation, Notifications), resetRoot: true);

    // Keep the sidebar highlight in sync with whatever page is showing
    // (including sub-navigation like asset detail → Swap).
    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NavigationService.CurrentPage))
        {
            return;
        }

        var title = Navigation.CurrentPage switch
        {
            WalletViewModel or AssetDetailViewModel or SendViewModel or ReceiveViewModel
                or ManageAssetsViewModel => "Wallet",
            SwapViewModel => "Swap",
            BuyCryptoViewModel => "Buy crypto",
            HistoryViewModel => "History",
            PerpsViewModel => "Perps",
            PolymarketViewModel => "Polymarket",
            StakingViewModel => "Staking",
            NftGalleryViewModel => "NFT gallery",
            SettingsViewModel => "Settings",
            _ => null
        };

        foreach (var n in NavItems)
        {
            n.IsActive = n.Title == title;
        }
    }
}
