using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using AtomicWallet.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Services;

/// <summary>
/// Holds the asset list and drives a lightweight local price simulation so the
/// table and charts feel alive. No network calls — all data is synthetic.
/// </summary>
public sealed partial class MarketDataService : ObservableObject, IDisposable
{
    private const int HistoryPoints = 24;

    private readonly DispatcherTimer _timer;
    private readonly Random _rng = new();

    /// <summary>When true, zero-balance assets are hidden from the wallet list.</summary>
    [ObservableProperty]
    private bool _hideZeroBalance;

    /// <summary>Persisted mirror of the in-app toast toggle (applied to NotificationService).</summary>
    [ObservableProperty]
    private bool _notificationsEnabled = true;

    public MarketDataService()
    {
        Assets = Seed();
        Transactions = SeedTransactions();
        PerpMarkets = SeedPerps();
        PolyCards = SeedPoly();
        BuyOrders = new ObservableCollection<BuyOrder>();
        LoadState();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += OnTick;
    }

    public ObservableCollection<Asset> Assets { get; }

    /// <summary>Full catalog of coins/tokens shown in Manage assets. Entries map to assets by Id.</summary>
    public IReadOnlyList<CatalogEntry> Catalog => CatalogData;

    public ObservableCollection<TxItem> Transactions { get; }

    public ObservableCollection<PerpMarket> PerpMarkets { get; }

    public ObservableCollection<PolyCard> PolyCards { get; }

    /// <summary>Card-purchase order history (Buy crypto → Order History).</summary>
    public ObservableCollection<BuyOrder> BuyOrders { get; }

    public decimal TotalValue => Assets.Sum(a => a.Value);

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    /// <summary>Simulated card purchase: credits balance, logs a tx and an order-history entry.</summary>
    public void Buy(Asset coin, decimal fiatUsd, decimal cryptoAmount)
    {
        if (coin == null || cryptoAmount <= 0)
        {
            return;
        }

        coin.Balance += cryptoAmount;
        AddTx(coin, TxDirection.Received, cryptoAmount, "Bank card purchase");
        BuyOrders.Insert(0, new BuyOrder
        {
            OrderId = "AW-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Ticker = coin.Ticker,
            LogoKey = coin.LogoKey,
            IconColor = coin.IconColor,
            CryptoAmount = cryptoAmount,
            FiatAmount = fiatUsd,
            Timestamp = DateTimeOffset.Now
        });
        Recompute();
    }

    /// <summary>Simulated deposit: credits the coin's balance and logs a Received tx.</summary>
    public void Deposit(Asset coin, decimal cryptoAmount)
    {
        if (coin == null || cryptoAmount <= 0)
        {
            return;
        }

        coin.Balance += cryptoAmount;
        AddTx(coin, TxDirection.Received, cryptoAmount, "Bank card purchase");
        Recompute();
    }

    /// <summary>Simulated send: debits the balance and logs a Sent tx.</summary>
    public bool Send(Asset coin, string toAddress, decimal amount)
    {
        if (coin == null || amount <= 0 || amount > coin.Balance)
        {
            return false;
        }

        coin.Balance -= amount;
        AddTx(coin, TxDirection.Sent, amount, string.IsNullOrWhiteSpace(toAddress) ? "External address" : toAddress);
        Recompute();
        return true;
    }

    /// <summary>Simulated swap: moves value from one asset to another at current prices.</summary>
    public bool Swap(Asset from, Asset to, decimal fromAmount)
    {
        if (from == null || to == null || from == to || fromAmount <= 0 || fromAmount > from.Balance
            || to.Price <= 0)
        {
            return false;
        }

        var fiat = fromAmount * from.Price;
        var toAmount = fiat / to.Price;
        from.Balance -= fromAmount;
        to.Balance += toAmount;
        AddTx(from, TxDirection.Sent, fromAmount, $"Swap to {to.Ticker}");
        AddTx(to, TxDirection.Received, toAmount, $"Swap from {from.Ticker}");
        Recompute();
        return true;
    }

    private void AddTx(Asset coin, TxDirection dir, decimal amount, string address)
    {
        Transactions.Insert(0, new TxItem
        {
            Ticker = coin.Ticker,
            LogoKey = coin.LogoKey,
            IconColor = coin.IconColor,
            Direction = dir,
            Amount = amount,
            Address = address,
            Hash = "demo" + System.Guid.NewGuid().ToString("N"),
            Timestamp = System.DateTimeOffset.Now
        });
    }

    /// <summary>Recomputes portfolio weights and notifies total changed (no persistence).</summary>
    public void RecomputeMarket()
    {
        var total = Assets.Sum(a => a.Value);
        foreach (var a in Assets)
        {
            a.PortfolioPercent = total > 0 ? System.Math.Round(a.Value / total * 100m, 2) : 0m;
        }

        OnPropertyChanged(nameof(TotalValue));
    }

    /// <summary>Recomputes weights and persists (used after a user mutation).</summary>
    private void Recompute()
    {
        RecomputeMarket();
        Save();
    }

    /// <summary>Persists balances, transactions and the display currency.</summary>
    public void Save()
    {
        var state = new SavedState
        {
            Currency = Fx.Instance.Code,
            HideZeroBalance = HideZeroBalance,
            NotificationsEnabled = NotificationsEnabled,
            HiddenIds = Assets.Where(a => !a.Visible).Select(a => a.Id).ToList()
        };
        foreach (var a in Assets)
        {
            state.Balances[a.Id] = a.Balance;
        }

        foreach (var o in BuyOrders)
        {
            state.BuyOrders.Add(new SavedBuyOrder
            {
                OrderId = o.OrderId,
                Ticker = o.Ticker,
                LogoKey = o.LogoKey,
                IconColor = o.IconColor,
                CryptoAmount = o.CryptoAmount,
                FiatAmount = o.FiatAmount,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status,
                Timestamp = o.Timestamp
            });
        }

        foreach (var t in Transactions)
        {
            state.Transactions.Add(new SavedTx
            {
                Ticker = t.Ticker,
                LogoKey = t.LogoKey,
                IconColor = t.IconColor,
                Direction = (int)t.Direction,
                Amount = t.Amount,
                Address = t.Address,
                Hash = t.Hash,
                Timestamp = t.Timestamp
            });
        }

        JsonStore.Save(state);
    }

    private void LoadState()
    {
        var s = JsonStore.Load();
        if (s == null)
        {
            return;
        }

        foreach (var a in Assets)
        {
            if (s.Balances.TryGetValue(a.Id, out var bal))
            {
                a.Balance = bal;
            }
        }

        // Restore which assets are shown. Null = legacy file: keep seeded defaults.
        if (s.HiddenIds != null)
        {
            var hidden = new HashSet<string>(s.HiddenIds);
            foreach (var a in Assets)
            {
                // Never hide an asset that holds a balance.
                a.Visible = a.Balance > 0 || !hidden.Contains(a.Id);
            }
        }

        HideZeroBalance = s.HideZeroBalance;
        NotificationsEnabled = s.NotificationsEnabled;

        if (s.BuyOrders is { Count: > 0 })
        {
            BuyOrders.Clear();
            foreach (var o in s.BuyOrders)
            {
                BuyOrders.Add(new BuyOrder
                {
                    OrderId = o.OrderId,
                    Ticker = o.Ticker,
                    LogoKey = o.LogoKey,
                    IconColor = o.IconColor,
                    CryptoAmount = o.CryptoAmount,
                    FiatAmount = o.FiatAmount,
                    PaymentMethod = o.PaymentMethod,
                    Status = o.Status,
                    Timestamp = o.Timestamp
                });
            }
        }

        if (s.Transactions is { Count: > 0 })
        {
            Transactions.Clear();
            foreach (var t in s.Transactions)
            {
                Transactions.Add(new TxItem
                {
                    Ticker = t.Ticker,
                    LogoKey = t.LogoKey,
                    IconColor = t.IconColor,
                    Direction = (TxDirection)t.Direction,
                    Amount = t.Amount,
                    Address = t.Address,
                    Hash = t.Hash,
                    Timestamp = t.Timestamp
                });
            }
        }

        if (!string.IsNullOrEmpty(s.Currency))
        {
            Fx.Instance.Set(s.Currency);
        }

        var total = Assets.Sum(a => a.Value);
        foreach (var a in Assets)
        {
            a.PortfolioPercent = total > 0 ? System.Math.Round(a.Value / total * 100m, 2) : 0m;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // Real prices, market caps and sparklines come from PriceFeedService; this only
        // adds a tiny between-update flicker so the live number feels alive without
        // drifting noticeably away from the real value.
        foreach (var a in Assets)
        {
            var volatility = a.Ticker.Contains("USDT") ? 0.0002 : 0.0009;
            var factor = 1 + ((_rng.NextDouble() - 0.5) * 2 * volatility);
            a.Price = Math.Round(a.Price * (decimal)factor, a.Price < 10 ? 8 : 2);
        }

        OnPropertyChanged(nameof(TotalValue));
    }

    private ObservableCollection<Asset> Seed()
    {
        var list = new ObservableCollection<Asset>
        {
            Make("btc", "Bitcoin", "BTC", null, "#F7931A", 73670.18m, -1.26m, 1476.04m, 0.0085m),
            Make("base", "BASE ETH", "ETH", "BASE", "#0052FF", 2015.91m, -0.67m, 243.29m),
            Make("ink", "INK", "ETH", "INK", "#7132F5", 2015.91m, -0.67m, 243.29m),
            Make("linea", "ETH Linea", "ETH", null, "#61DFFF", 2015.91m, -0.67m, 243.29m),
            Make("katana", "ETH Katana", "ETH", null, "#2F6FF5", 2015.91m, -0.67m, 243.29m),
            Make("mega", "MegaETH", "ETH", null, "#8A93B2", 2015.91m, -0.67m, 243.29m),
            Make("eth", "Ethereum", "ETH", null, "#627EEA", 2015.91m, -0.67m, 243.29m, 0.12m),
            Make("op", "Ethereum OP", "ETHOP", "OP", "#FF0420", 2015.91m, -0.67m, 243.29m),
            Make("arb", "Ethereum ARB", "ETHARB", "ARB", "#28A0F0", 2015.91m, -0.67m, 243.29m),
            Make("usdt-trx", "Tether USD", "TRX-USDT", "TRX", "#26A17B", 1.00m, 0.02m, 189.32m),
            Make("usdt-pol", "Tether USD", "USDT", "POL", "#26A17B", 1.00m, 0.02m, 189.32m, 40m),
            Make("bnb", "BNB", "BNB", null, "#F3BA2F", 585.40m, 0.63m, 84.60m),
            Make("sol", "Solana", "SOL", null, "#9945FF", 150.20m, 4.21m, 70.10m, 3.5m),
            Make("xrp", "XRP", "XRP", null, "#23A1D6", 0.52m, 2.07m, 29.80m),
        };

        // Add the rest of the catalog as hidden, zero-balance assets so Manage assets
        // can reveal them. Entries whose Id already exists (BTC/ETH/…) are skipped.
        foreach (var c in CatalogData)
        {
            if (list.Any(a => a.Id == c.Id))
            {
                continue;
            }

            var asset = new Asset
            {
                Id = c.Id,
                Name = c.Name,
                Ticker = c.Ticker,
                IconColor = c.Color,
                LogoKey = c.Ticker.ToLowerInvariant(),
                Balance = 0m,
                Price = c.Price,
                Change24h = 0m,
                PortfolioPercent = 0m,
                MarketCapB = 0m,
                Visible = false,
                SparkColor = "#3FD08F"
            };
            asset.PriceHistory = BuildHistory(c.Price, 0m);
            list.Add(asset);
        }

        var total = list.Sum(a => a.Value);
        if (total > 0)
        {
            foreach (var a in list)
            {
                a.PortfolioPercent = System.Math.Round(a.Value / total * 100m, 2);
            }
        }

        return list;
    }

    /// <summary>
    /// The Manage-assets catalog. The first six map onto already-seeded wallet assets
    /// (so they link, not duplicate); the rest become hidden zero-balance rows.
    /// </summary>
    private static readonly CatalogEntry[] CatalogData =
    {
        // Coins
        new("btc", "Bitcoin", "BTC", "#F7931A", 73670.18m, false),
        new("eth", "Ethereum", "ETH", "#627EEA", 2015.91m, false),
        new("bnb", "BNB", "BNB", "#F3BA2F", 585.40m, false),
        new("sol", "Solana", "SOL", "#14F195", 150.20m, false),
        new("xrp", "XRP", "XRP", "#23A1D6", 0.52m, false),
        new("usdt-pol", "Tether USD", "USDT", "#26A17B", 1.00m, false),
        new("cat-ada", "Cardano", "ADA", "#0033AD", 0.45m, false),
        new("cat-akt", "Akash Network", "AKT", "#FF414C", 3.20m, false),
        new("cat-algo", "Algorand", "ALGO", "#2C2C2C", 0.18m, false),
        new("cat-apt", "Aptos", "APT", "#2D2D2D", 8.50m, false),
        new("cat-atom", "Cosmos", "ATOM", "#2E3148", 7.20m, false),
        new("cat-avax", "Avalanche", "AVAX", "#E84142", 28.00m, false),
        new("cat-axl", "Axelar", "AXL", "#3B3B3B", 0.65m, false),
        new("cat-bch", "Bitcoin Cash", "BCH", "#0AC18E", 380.00m, false),
        new("cat-bsv", "Bitcoin SV", "BSV", "#EAB300", 55.00m, false),
        new("cat-cro", "Cronos", "CRO", "#103F68", 0.12m, false),
        new("cat-dash", "Dash", "DASH", "#008CE7", 32.00m, false),
        new("cat-doge", "Dogecoin", "DOGE", "#C2A633", 0.16m, false),
        new("cat-dot", "Polkadot", "DOT", "#E6007A", 6.80m, false),
        new("cat-egld", "MultiversX", "EGLD", "#1B46C2", 35.00m, false),
        new("cat-eos", "EOS", "EOS", "#2C2C2C", 0.78m, false),
        new("cat-etc", "Ethereum Classic", "ETC", "#328332", 26.00m, false),
        new("cat-fil", "Filecoin", "FIL", "#0090FF", 5.20m, false),
        new("cat-hbar", "Hedera", "HBAR", "#2C2C2C", 0.27m, false),
        new("cat-ltc", "Litecoin", "LTC", "#345D9D", 88.00m, false),
        new("cat-neo", "Neo", "NEO", "#00E599", 12.00m, false),
        new("cat-trx", "Tron", "TRX", "#EB0029", 0.13m, false),
        new("cat-vet", "VeChain", "VET", "#15BDFF", 0.028m, false),
        new("cat-xlm", "Stellar", "XLM", "#14B6E7", 0.11m, false),
        new("cat-xmr", "Monero", "XMR", "#FF6600", 165.00m, false),

        // Tokens
        new("cat-aave", "Aave", "AAVE", "#B6509E", 95.00m, true),
        new("cat-ant", "Aragon", "ANT", "#00CBE6", 6.50m, true),
        new("cat-ape", "ApeCoin", "APE", "#0054F9", 1.20m, true),
        new("cat-arb", "Arbitrum", "ARB", "#2D374B", 0.85m, true),
        new("cat-axs", "Axie Infinity", "AXS", "#0055D5", 6.00m, true),
        new("cat-bat", "Basic Attention", "BAT", "#FF5000", 0.22m, true),
        new("cat-busd", "Binance USD", "BUSD", "#F0B90B", 1.00m, true),
        new("cat-cake", "PancakeSwap", "CAKE", "#D1884F", 2.10m, true),
        new("cat-comp", "Compound", "COMP", "#00D395", 48.00m, true),
        new("cat-crv", "Curve DAO", "CRV", "#40649F", 0.52m, true),
        new("cat-dai", "Dai", "DAI", "#F5AC37", 1.00m, true),
        new("cat-ens", "Ethereum Name Service", "ENS", "#5298FF", 22.00m, true),
        new("cat-fet", "Fetch.ai", "FET", "#2A2A4A", 1.30m, true),
        new("cat-grt", "The Graph", "GRT", "#6F4CFF", 0.15m, true),
        new("cat-inj", "Injective", "INJ", "#00D2FF", 24.00m, true),
        new("cat-link", "Chainlink", "LINK", "#2A5ADA", 14.00m, true),
        new("cat-mkr", "Maker", "MKR", "#1AAB9B", 1450.00m, true),
        new("cat-op", "Optimism", "OP", "#FF0420", 2.40m, true),
        new("cat-sand", "The Sandbox", "SAND", "#00ADEF", 0.42m, true),
        new("cat-shib", "Shiba Inu", "SHIB", "#FFA409", 0.000022m, true),
        new("cat-snx", "Synthetix", "SNX", "#00D1FF", 2.80m, true),
        new("cat-sushi", "SushiSwap", "SUSHI", "#FA52A0", 1.10m, true),
        new("cat-uni", "Uniswap", "UNI", "#FF007A", 7.50m, true),
        new("cat-usdc", "USD Coin", "USDC", "#2775CA", 1.00m, true),
        new("cat-yfi", "yearn.finance", "YFI", "#006AE3", 6800.00m, true),
        new("cat-zrx", "0x Protocol", "ZRX", "#302C2C", 0.38m, true)
    };

    private Asset Make(string id, string name, string ticker, string? badge, string color,
        decimal price, decimal change, decimal marketCapB, decimal balance = 0m)
    {
        var asset = new Asset
        {
            Id = id,
            Name = name,
            Ticker = ticker,
            ChainBadge = badge,
            IconColor = color,
            LogoKey = LogoFor(id),
            Balance = balance,
            Price = price,
            Change24h = change,
            PortfolioPercent = 0m,
            MarketCapB = marketCapB,
            SparkColor = change >= 0 ? "#3FD08F" : "#FF6B6B"
        };
        asset.PriceHistory = BuildHistory(price, change);
        return asset;
    }

    private ObservableCollection<TxItem> SeedTransactions()
    {
        var now = DateTimeOffset.Now;
        return new ObservableCollection<TxItem>
        {
            Tx("SOL", "sol", "#14F195", TxDirection.Received, 0.5m, 2, "8x12ESD1CCauvt7y4SdfHSAY63S3rcg"),
            Tx("SOL", "sol", "#14F195", TxDirection.Sent, 0.500055m, 30, "5Q24ozR96dCfWxQ8FzsqGH15AY39y2"),
            Tx("USDT", "usdt", "#26A17B", TxDirection.Received, 5.2421m, 60, "0x0b0c44cadd5a4f3892a87598c68be"),
            Tx("BTC", "btc", "#F7931A", TxDirection.Received, 0.0021m, 90, "bc1qxy2kgdygjrsqtzq2n0yrf2493p83"),
            Tx("ETH", "eth", "#627EEA", TxDirection.Sent, 0.18m, 180, "0x71C7656EC7ab88b098defB751B7401"),
            Tx("XRP", "xrp", "#23A1D6", TxDirection.Received, 120.50m, 320, "rDemoExampleAddressOnly0000Xyz9"),
            Tx("BTC", "btc", "#F7931A", TxDirection.Sent, 0.0009m, 540, "bc1qardexpl0rexampledem0address0"),
        };

        TxItem Tx(string ticker, string logo, string color, TxDirection dir, decimal amt, int hoursAgo, string addr) => new()
        {
            Ticker = ticker,
            LogoKey = logo,
            IconColor = color,
            Direction = dir,
            Amount = amt,
            Address = addr,
            Hash = "6453c2d920b5e9bb6570bf6c74156c32fd2c21578cf1c01f8e1ed1d98327afab",
            Timestamp = now.AddHours(-hoursAgo)
        };
    }

    private ObservableCollection<PolyCard> SeedPoly() => new()
    {
        new() { Title = "Bitcoin above threshold by month end?", IconColor = "#F7931A", Initial = "B", Chance = 73 },
        new() { Title = "Bitcoin up or down today?", IconColor = "#F7931A", Initial = "B", Chance = 50, YesLabel = "Up", NoLabel = "Down" },
        new() { Title = "XRP up or down today?", IconColor = "#23A1D6", Initial = "X", Chance = 50, YesLabel = "Up", NoLabel = "Down" },
        new() { Title = "Ethereum above threshold this week?", IconColor = "#627EEA", Initial = "E", Chance = 41 },
        new() { Title = "Will the index close green?", IconColor = "#3FB37F", Initial = "S", Chance = 62 },
        new() { Title = "Top team wins the match?", IconColor = "#8785FF", Initial = "T", Chance = 54 },
    };

    private ObservableCollection<PerpMarket> SeedPerps() => new()
    {
        new() { Name = "BTC-USD", LogoKey = "btc", IconColor = "#F7931A", Leverage = "40x", Price = 73867m, Change24h = 0.482m, Volume = "$1B", FundingRate = 0.0013m, OpenInterest = "$2.2B" },
        new() { Name = "HYPE-USD", IconColor = "#2EC7A8", Leverage = "10x", Price = 69.375m, Change24h = 5.067m, Volume = "$929.6M", FundingRate = 0.0028m, OpenInterest = "$1.5B" },
        new() { Name = "ETH-USD", LogoKey = "eth", IconColor = "#627EEA", Leverage = "25x", Price = 2025m, Change24h = 0.491m, Volume = "$331.1M", FundingRate = 0.0013m, OpenInterest = "$1.3B" },
        new() { Name = "SP500-USD", IconColor = "#3FB37F", Leverage = "50x", Price = 7605m, Change24h = 0.266m, Volume = "$47.1M", FundingRate = 0.0050m, OpenInterest = "$495.7M" },
        new() { Name = "SOL-USD", LogoKey = "sol", IconColor = "#14F195", Leverage = "20x", Price = 82.722m, Change24h = 0.474m, Volume = "$73.6M", FundingRate = 0.0013m, OpenInterest = "$323.2M" },
        new() { Name = "BRENTOIL-USD", IconColor = "#C28F4B", Leverage = "20x", Price = 92.521m, Change24h = 0.680m, Volume = "$20.9M", FundingRate = -0.0010m, OpenInterest = "$297.4M" },
        new() { Name = "ZEC-USD", LogoKey = "zec", IconColor = "#ECB244", Leverage = "10x", Price = 548.4m, Change24h = 5.765m, Volume = "$136.7M", FundingRate = 0.0013m, OpenInterest = "$256.6M" },
        new() { Name = "NVDA-USD", IconColor = "#76B900", Leverage = "20x", Price = 217.8m, Change24h = 1.401m, Volume = "$10.1M", FundingRate = 0.0054m, OpenInterest = "$183M" },
        new() { Name = "XRP-USD", LogoKey = "xrp", IconColor = "#23A1D6", Leverage = "20x", Price = 1.3358m, Change24h = -0.447m, Volume = "$46.3M", FundingRate = -0.0001m, OpenInterest = "$87.8M" },
        new() { Name = "GOLD-USD", IconColor = "#E1B12C", Leverage = "25x", Price = 4543m, Change24h = 0.085m, Volume = "$6.5M", FundingRate = 0.0006m, OpenInterest = "$91.5M" },
    };

    /// <summary>Maps an asset id to its coin-logo filename key (Assets/coins/{key}.png).</summary>
    private static string LogoFor(string id) => id switch
    {
        "btc" => "btc",
        "bnb" => "bnb",
        "sol" => "sol",
        "xrp" => "xrp",
        _ when id.StartsWith("usdt") => "usdt",
        _ => "eth"
    };

    private ObservableCollection<decimal> BuildHistory(decimal price, decimal change)
    {
        var history = new ObservableCollection<decimal>();
        var start = price / (1 + (change / 100m));
        for (var i = 0; i < HistoryPoints; i++)
        {
            var t = (decimal)i / (HistoryPoints - 1);
            var trend = start + ((price - start) * t);
            var noise = (decimal)((_rng.NextDouble() - 0.5) * 0.03) * price;
            history.Add(Math.Max(0, trend + noise));
        }

        history[^1] = price;
        return history;
    }

    public void Dispose()
    {
        _timer.Tick -= OnTick;
        _timer.Stop();
    }
}

/// <summary>One coin/token in the Manage-assets catalog.</summary>
public sealed record CatalogEntry(string Id, string Name, string Ticker, string Color, decimal Price, bool IsToken);
