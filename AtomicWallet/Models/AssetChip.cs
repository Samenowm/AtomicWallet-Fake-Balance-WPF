using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Models;

/// <summary>A toggleable asset chip in the Manage Assets grid, bound to a wallet asset.</summary>
public partial class AssetChip : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>The wallet asset this chip toggles the visibility of.</summary>
    public Asset? Asset { get; init; }

    public string Ticker { get; init; } = string.Empty;
    public string IconColor { get; init; } = "#3A6FF8";
    public string LogoKey { get; init; } = string.Empty;

    /// <summary>True when the asset holds a balance (cannot be hidden).</summary>
    public bool HasBalance => Asset is { Balance: > 0 };
}
