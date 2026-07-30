using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Models;

/// <summary>
/// A single row in the wallet asset table. This is a UI re-creation: balances
/// start empty and prices/market data are simulated locally — there is no real
/// blockchain or network connection.
/// </summary>
public partial class Asset : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _ticker = string.Empty;

    /// <summary>Optional network/chain tag rendered as a pill (e.g. "BASE", "OP").</summary>
    [ObservableProperty] private string? _chainBadge;

    /// <summary>Hex color of the round coin icon (fallback letter badge).</summary>
    [ObservableProperty] private string _iconColor = "#3A6FF8";

    /// <summary>Filename key (lowercase) into Assets/coins/{key}.png, e.g. "btc".</summary>
    [ObservableProperty] private string _logoKey = string.Empty;

    /// <summary>Whether this asset is shown in the wallet list (toggled in Manage assets).</summary>
    [ObservableProperty] private bool _visible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Value))]
    private decimal _balance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Value))]
    private decimal _price;

    /// <summary>24h price change in percent.</summary>
    [ObservableProperty] private decimal _change24h;

    /// <summary>Share of the whole portfolio in percent.</summary>
    [ObservableProperty] private decimal _portfolioPercent;

    /// <summary>Market capitalization in billions of USD.</summary>
    [ObservableProperty] private decimal _marketCapB;

    [ObservableProperty] private ObservableCollection<decimal> _priceHistory = new();

    /// <summary>Hex color used for the 7-day sparkline.</summary>
    [ObservableProperty] private string _sparkColor = "#5B7CFF";

    public decimal Value => Balance * Price;
}
