using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AtomicWallet.Services;

/// <summary>
/// Pulls real market data from CoinGecko's public API and applies it to the wallet:
/// prices, 24h change, market cap and 7-day sparklines (one batched call), plus live
/// fiat exchange rates and on-demand price-chart history. Network errors are swallowed —
/// the app keeps its last known data, so it still runs offline.
/// </summary>
public sealed class PriceFeedService : IDisposable
{
    private const string Api = "https://api.coingecko.com/api/v3";

    private static readonly HttpClient Http = CreateClient();

    private readonly MarketDataService _market;
    private readonly DispatcherTimer _timer;

    /// <summary>Reachable from view models (e.g. the chart) without DI plumbing.</summary>
    public static PriceFeedService? Instance { get; private set; }

    /// <summary>Maps each wallet asset id to its CoinGecko coin id.</summary>
    private static readonly Dictionary<string, string> GeckoIds = new()
    {
        ["btc"] = "bitcoin",
        ["eth"] = "ethereum",
        ["base"] = "ethereum",
        ["ink"] = "ethereum",
        ["linea"] = "ethereum",
        ["katana"] = "ethereum",
        ["mega"] = "ethereum",
        ["op"] = "ethereum",
        ["arb"] = "ethereum",
        ["usdt-trx"] = "tether",
        ["usdt-pol"] = "tether",
        ["bnb"] = "binancecoin",
        ["sol"] = "solana",
        ["xrp"] = "ripple",
        ["cat-ada"] = "cardano",
        ["cat-akt"] = "akash-network",
        ["cat-algo"] = "algorand",
        ["cat-apt"] = "aptos",
        ["cat-atom"] = "cosmos",
        ["cat-avax"] = "avalanche-2",
        ["cat-axl"] = "axelar",
        ["cat-bch"] = "bitcoin-cash",
        ["cat-bsv"] = "bitcoin-cash-sv",
        ["cat-cro"] = "crypto-com-chain",
        ["cat-dash"] = "dash",
        ["cat-doge"] = "dogecoin",
        ["cat-dot"] = "polkadot",
        ["cat-egld"] = "elrond-erd-2",
        ["cat-eos"] = "eos",
        ["cat-etc"] = "ethereum-classic",
        ["cat-fil"] = "filecoin",
        ["cat-hbar"] = "hedera-hashgraph",
        ["cat-ltc"] = "litecoin",
        ["cat-neo"] = "neo",
        ["cat-trx"] = "tron",
        ["cat-vet"] = "vechain",
        ["cat-xlm"] = "stellar",
        ["cat-xmr"] = "monero",
        ["cat-aave"] = "aave",
        ["cat-ant"] = "aragon",
        ["cat-ape"] = "apecoin",
        ["cat-arb"] = "arbitrum",
        ["cat-axs"] = "axie-infinity",
        ["cat-bat"] = "basic-attention-token",
        ["cat-busd"] = "binance-usd",
        ["cat-cake"] = "pancakeswap-token",
        ["cat-comp"] = "compound-governance-token",
        ["cat-crv"] = "curve-dao-token",
        ["cat-dai"] = "dai",
        ["cat-ens"] = "ethereum-name-service",
        ["cat-fet"] = "fetch-ai",
        ["cat-grt"] = "the-graph",
        ["cat-inj"] = "injective-protocol",
        ["cat-link"] = "chainlink",
        ["cat-mkr"] = "maker",
        ["cat-op"] = "optimism",
        ["cat-sand"] = "the-sandbox",
        ["cat-shib"] = "shiba-inu",
        ["cat-snx"] = "havven",
        ["cat-sushi"] = "sushi",
        ["cat-uni"] = "uniswap",
        ["cat-usdc"] = "usd-coin",
        ["cat-yfi"] = "yearn-finance",
        ["cat-zrx"] = "0x"
    };

    public PriceFeedService(MarketDataService market)
    {
        _market = market;
        Instance = this;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += async (_, _) => await RefreshAsync();
    }

    public bool HasLiveData { get; private set; }

    public void Start()
    {
        _ = RefreshAsync();
        _timer.Start();
    }

    /// <summary>Pulls market data + FX rates and applies them on the UI thread.</summary>
    public async Task RefreshAsync()
    {
        await RefreshMarketsAsync();
        await RefreshFxAsync();
    }

    private async Task RefreshMarketsAsync()
    {
        try
        {
            var ids = string.Join(",", GeckoIds.Values.Distinct());
            var url = $"{Api}/coins/markets?vs_currency=usd&ids={ids}" +
                      "&sparkline=true&price_change_percentage=24h&per_page=250&page=1";

            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // id -> (price, change, marketCap, sparkline)
            var map = new Dictionary<string, (decimal Price, decimal? Change, decimal? Cap, decimal[]? Spark)>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("id", out var idEl)) continue;
                var id = idEl.GetString();
                if (string.IsNullOrEmpty(id)) continue;

                var price = Num(el, "current_price") ?? 0m;
                var change = Num(el, "price_change_percentage_24h");
                var cap = Num(el, "market_cap");
                decimal[]? spark = null;
                if (el.TryGetProperty("sparkline_in_7d", out var s) &&
                    s.TryGetProperty("price", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    spark = arr.EnumerateArray()
                        .Where(p => p.ValueKind == JsonValueKind.Number)
                        .Select(p => p.GetDecimal())
                        .ToArray();
                }

                map[id] = (price, change, cap, spark);
            }

            Application.Current?.Dispatcher.Invoke(() => ApplyMarkets(map));
        }
        catch
        {
            // Offline / rate-limited / parse error — keep the last known data.
        }
    }

    private void ApplyMarkets(
        Dictionary<string, (decimal Price, decimal? Change, decimal? Cap, decimal[]? Spark)> map)
    {
        var changed = false;

        foreach (var asset in _market.Assets)
        {
            if (!GeckoIds.TryGetValue(asset.Id, out var gid) || !map.TryGetValue(gid, out var d))
            {
                continue;
            }

            if (d.Price > 0)
            {
                asset.Price = d.Price < 1m ? Math.Round(d.Price, 8) : Math.Round(d.Price, 2);
                changed = true;
            }

            if (d.Change is { } chg)
            {
                asset.Change24h = Math.Round(chg, 2);
                asset.SparkColor = chg >= 0 ? "#3FD08F" : "#FF6B6B";
            }

            if (d.Cap is { } cap)
            {
                asset.MarketCapB = Math.Round(cap / 1_000_000_000m, 2);
            }

            if (d.Spark is { Length: > 1 })
            {
                asset.PriceHistory = new ObservableCollection<decimal>(Downsample(d.Spark, 48));
            }
        }

        if (changed)
        {
            HasLiveData = true;
            _market.RecomputeMarket();
        }
    }

    private async Task RefreshFxAsync()
    {
        try
        {
            var url = $"{Api}/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp,jpy,try";
            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("bitcoin", out var btc)) return;

            var usd = Num(btc, "usd") ?? 0m;
            if (usd <= 0) return;

            // USD → currency = (BTC priced in currency) / (BTC priced in USD).
            var rates = new Dictionary<string, decimal> { ["USD"] = 1m, ["BTC"] = 1m / usd };
            foreach (var code in new[] { "eur", "gbp", "jpy", "try" })
            {
                var v = Num(btc, code);
                if (v is { } x && x > 0)
                {
                    rates[code.ToUpperInvariant()] = x / usd;
                }
            }

            Application.Current?.Dispatcher.Invoke(() => Fx.Instance.UpdateRates(rates));
        }
        catch
        {
            // keep demo rates
        }
    }

    /// <summary>
    /// Fetches real historical prices for an asset over a range (24H/1W/1M/1Y/ALL).
    /// Returns null on failure so callers can keep their fallback series.
    /// </summary>
    public async Task<IReadOnlyList<decimal>?> GetHistoryAsync(string assetId, string range)
    {
        if (!GeckoIds.TryGetValue(assetId, out var gid))
        {
            return null;
        }

        var days = range switch
        {
            "24H" => "1",
            "1W" => "7",
            "1M" => "30",
            "1Y" => "365",
            "ALL" => "max",
            _ => "7"
        };

        try
        {
            var url = $"{Api}/coins/{gid}/market_chart?vs_currency=usd&days={days}";
            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("prices", out var prices) ||
                prices.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var series = prices.EnumerateArray()
                .Where(p => p.ValueKind == JsonValueKind.Array && p.GetArrayLength() >= 2)
                .Select(p => p[1].GetDecimal())
                .ToArray();

            return series.Length > 1 ? Downsample(series, 220) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Evenly reduces a series to at most <paramref name="max"/> points.</summary>
    private static List<decimal> Downsample(decimal[] src, int max)
    {
        if (src.Length <= max)
        {
            return src.ToList();
        }

        var result = new List<decimal>(max);
        var step = (double)(src.Length - 1) / (max - 1);
        for (var i = 0; i < max; i++)
        {
            result.Add(src[(int)Math.Round(i * step)]);
        }

        return result;
    }

    private static decimal? Num(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetDecimal()
            : null;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.Add("User-Agent", "AtomicWalletUI/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    public void Dispose() => _timer.Stop();
}
