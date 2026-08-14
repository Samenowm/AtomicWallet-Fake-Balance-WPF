using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Services;

/// <summary>
/// Display-currency state. A single shared instance drives all fiat displays via the
/// FiatConverter; changing it refreshes bindings live. USD→currency rates start at demo
/// values and are replaced with live rates by <see cref="PriceFeedService"/>.
/// </summary>
public sealed partial class Fx : ObservableObject
{
    public static Fx Instance { get; } = new();

    private static readonly Dictionary<string, string> Symbols = new()
    {
        ["USD"] = "$",
        ["EUR"] = "€",
        ["GBP"] = "£",
        ["JPY"] = "¥",
        ["TRY"] = "₺",
        ["BTC"] = "₿"
    };

    private readonly Dictionary<string, decimal> _rates = new()
    {
        ["USD"] = 1m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
        ["JPY"] = 156m,
        ["TRY"] = 32.5m,
        ["BTC"] = 0.0000135m
    };

    [ObservableProperty] private string _code = "USD";
    [ObservableProperty] private string _symbol = "$";
    [ObservableProperty] private decimal _rate = 1m;

    public void Set(string code)
    {
        if (!string.IsNullOrEmpty(code) && Symbols.TryGetValue(code, out var symbol))
        {
            Code = code;
            Symbol = symbol;
            Rate = _rates.TryGetValue(code, out var r) ? r : 1m;
        }
    }

    /// <summary>Replaces the live USD→currency rates and re-applies the active one.</summary>
    public void UpdateRates(IReadOnlyDictionary<string, decimal> usdRates)
    {
        foreach (var (code, rate) in usdRates)
        {
            if (rate > 0)
            {
                _rates[code] = rate;
            }
        }

        if (_rates.TryGetValue(Code, out var current))
        {
            Rate = current;
        }
    }
}
