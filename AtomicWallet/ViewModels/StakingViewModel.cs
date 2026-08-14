using System.Collections.ObjectModel;
using System.Linq;
using AtomicWallet.Models;
using AtomicWallet.Services;

namespace AtomicWallet.ViewModels;

/// <summary>A stakeable asset row with a demo APR.</summary>
public sealed class StakeOption
{
    public Asset Coin { get; init; } = null!;
    public decimal Apr { get; init; }
    public string AprText => $"{Apr:0.0}% APR";
}

/// <summary>Staking screen: list of stakeable assets with demo APRs.</summary>
public sealed class StakingViewModel : ViewModelBase
{
    public StakingViewModel(MarketDataService market)
    {
        var aprs = new System.Collections.Generic.Dictionary<string, decimal>
        {
            ["sol"] = 7.1m, ["eth"] = 4.3m, ["ada"] = 3.2m, ["xrp"] = 0m,
            ["bnb"] = 5.6m, ["usdt-trx"] = 8.0m
        };

        Options = new ObservableCollection<StakeOption>(
            market.Assets
                .Where(a => aprs.ContainsKey(a.Id) && aprs[a.Id] > 0)
                .Select(a => new StakeOption { Coin = a, Apr = aprs[a.Id] }));
    }

    public ObservableCollection<StakeOption> Options { get; }
}
