# Architecture

This document explains how the application is put together: the MVVM layers, how
screens are composed and navigated, how the simulated market data flows through the
app, and how state is persisted.

> Reminder: this is a UI study. There is no backend and no chain. Live prices are read
> from a public API (CoinGecko); everything else — balances, transactions, addresses —
> is a local simulation.

## Overview

The app follows a strict **MVVM** structure with **constructor dependency injection**.
Three singleton services hold all shared state and behavior; view models orchestrate
them and expose bindable state; views are passive XAML bound to those view models.

```
App (DI container)
 ├─ MarketDataService     (asset catalog + balances + persistence)
 ├─ PriceFeedService      (live CoinGecko prices → MarketDataService)
 ├─ NavigationService     (which page is shown; back stack)
 ├─ NotificationService   (in-app toasts)
 └─ ShellViewModel ──▶ MainWindow
                          └─ ContentControl ──▶ current page view model ──▶ View
```

## Layers

```
Models/        Plain data objects (POCO/ObservableObject). No UI, no services.
Services/      Singletons that own state and cross-screen behavior.
ViewModels/    One per screen; bindable state + commands. Depend on services.
Views/         XAML UserControls; no logic beyond view concerns.
Controls/      Custom-drawn FrameworkElements (charts).
Converters/    IValueConverter / IMultiValueConverter helpers.
Themes/        ResourceDictionaries: colors, typography, icons, control styles.
```

### Models

Lightweight data carriers. Those that change at runtime derive from
`ObservableObject` (CommunityToolkit) so the UI updates automatically.

- **`Asset`** — a wallet row: id, name, ticker, optional chain badge, icon color/logo
  key, balance, price, 24h change, portfolio %, market cap, price history, spark color
  and a `Visible` flag (used by Manage assets). Exposes a computed `Value = Balance × Price`.
- **`TxItem`** — a transaction (direction, amount, address, hash, timestamp).
- **`BuyOrder`** — a card-purchase entry for the Buy crypto order history.
- **`PerpMarket`**, **`PolyCard`** — rows for the Perps and Polymarket screens.
- **`AssetChip`** — a toggle chip in Manage assets, bound to its backing `Asset`.

### Services

- **`MarketDataService`** (singleton, `ObservableObject`)
  - Seeds the asset catalog (coins + tokens) with starting prices and a few demo
    balances; the rest start hidden and zero-balance so Manage assets can reveal them.
  - Runs a `DispatcherTimer` that adds a tiny flicker between live-price updates and
    feeds the sparkline history.
  - Owns the mutation API: `Buy`, `Deposit`, `Send`, `Swap`, plus `RecomputeMarket`
    (recalculates portfolio weights and the total) and `Recompute` (the same, then saves).
  - Holds the order history (`BuyOrders`) and the `HideZeroBalance` /
    `NotificationsEnabled` preferences.
  - Loads and saves all of the above via `JsonStore`.
- **`PriceFeedService`** (singleton; also exposed via a static `Instance`)
  - Maps each asset id to its CoinGecko coin id and, every 30 seconds (plus once at
    startup), fetches prices, 24h changes, market caps and 7-day sparklines in a single
    `/coins/markets` request, and live fiat rates in a second small request.
  - Applies the results on the UI thread (`Price`, `Change24h`, `MarketCapB`,
    `PriceHistory`, `Fx` rates) and calls `RecomputeMarket`.
  - `GetHistoryAsync(assetId, range)` fetches real `/market_chart` history for the asset
    detail price chart on demand.
  - Any failure (offline, rate-limit, parse) is swallowed, so the app keeps its last
    known data and never blocks the UI.
- **`NavigationService`** — holds the `CurrentPage` view model and a back stack.
  Top-level (sidebar) navigation resets the stack; in-page navigation
  (coin → detail → send) pushes onto it so the back button works.
- **`NotificationService`** — a single in-app toast (message + visibility) on an
  auto-hide timer; can be globally disabled.
- **`JsonStore`** + **`Fx`** — JSON load/save helpers and the shared display-currency
  singleton (code, symbol, rate; raises changes so bound fiat values re-convert live).

### ViewModels

One view model per screen, all deriving from a small `ViewModelBase`. They depend only
on services (injected) and expose:

- Observable properties (`[ObservableProperty]`) for bindable state.
- Commands (`[RelayCommand]`) for user actions, with `CanExecute` guards where relevant
  (e.g. Swap/Send are disabled until the amount is valid).
- `ICollectionView` wrappers for sortable/searchable/filterable tables.

`ShellViewModel` owns the sidebar nav list, the selected currency, the portfolio
shortcut and the exit-confirmation flow.

### Views

XAML `UserControl`s, one per screen, bound to their view model via `DataContext`. The
mapping from view model → view is declared once in `App.xaml` as implicit
`DataTemplate`s, so navigation only ever swaps a view model and WPF resolves the view.

```xml
<DataTemplate DataType="{x:Type vm:WalletViewModel}">
    <views:WalletView />
</DataTemplate>
```

`MainWindow` is the shell: custom title bar + sidebar + a `ContentControl` whose
`Content` is bound to `NavigationService.CurrentPage`.

## Navigation

`MainWindow` hosts a single `ContentControl` bound to the current page view model:

```
ContentControl.Content  ←  NavigationService.CurrentPage
```

- **Sidebar items** call `Navigate(page, resetRoot: true)` — a fresh root, back stack
  cleared.
- **In-page links** (open a coin, go to Send/Receive/Swap) call `Navigate(page)` which
  pushes the previous page so `GoBack()` returns to it.

Because views are resolved from view-model type via `DataTemplate`, navigation code
never references views directly.

## Data & simulation flow

```
PriceFeedService timer (every 30s) + startup
   └─ GET CoinGecko /coins/markets  → Price / Change24h / MarketCapB / PriceHistory
   └─ GET CoinGecko /simple/price   → Fx rates
        └─ RecomputeMarket() → ObservableObject change notifications
             └─ Bindings refresh tables, totals, sparklines, fiat values

MarketDataService timer (every ~Ns)
   └─ tiny price flicker (between live updates)

User action (Buy / Swap / Send)
   └─ ViewModel command → MarketDataService.Buy/Swap/Send
        └─ mutates balances, appends TxItem / BuyOrder, Recompute()
             └─ Save() → state.json   +   UI refresh
```

The currency layer is independent: changing `Fx` (the display currency) raises a change
that every fiat `MultiBinding` listens to, so values re-convert without touching the
underlying USD amounts.

### Price chart series

When a range is selected (24H / 1W / 1M / 1Y / ALL) the chart first renders a synthetic
**geometric random walk** seeded from `(coin id, range)` for an instant, natural-looking
line, then asynchronously fetches **real `/market_chart` history** and swaps it in (a
request token guards against stale responses when the user switches quickly). If the
fetch fails it simply keeps the synthetic line. A new list instance is assigned each
time so the `AreaChart` re-renders.

## Persistence

State is serialized to `%AppData%/AtomicWallet/state.json` via `System.Text.Json`.
Saved on every mutation and on exit; loaded on startup.

| Field | Purpose |
|-------|---------|
| `Balances` | Per-asset balance (by id) |
| `Transactions` | Transaction history |
| `Currency` | Selected display currency |
| `HiddenIds` | Assets hidden from the wallet (`null` on legacy files = use defaults) |
| `HideZeroBalance` | "Hide zero balance" preference |
| `NotificationsEnabled` | Toast on/off |
| `BuyOrders` | Buy-crypto order history |

Loading is backward-compatible: missing fields fall back to seeded defaults, and an
asset with a balance is never hidden.

## Custom-drawn controls

To avoid a charting dependency, three controls override `OnRender` and draw directly:

- **`Sparkline`** — the compact 7-day line in each wallet row.
- **`AreaChart`** — the asset price chart: a line plus a soft vertical gradient fill.
- **`DonutChart`** — the portfolio ring, one arc segment per holding.

Each takes its data through dependency properties marked `AffectsRender`, so assigning
a new series triggers a redraw.

## Theming

All visuals come from resource dictionaries merged in `App.xaml`:

- **Colors** — semantic color + brush tokens (background, surfaces, text, accents,
  positive/negative).
- **Typography** — Roboto-based text styles (captions, headings, hero numbers).
- **Icons** — navigation/action glyphs as vector `Path` geometries.
- **Controls** — implicit and keyed styles for buttons, inputs, switches, the combo
  box, chain badges, the slim scrollbar and the window caption buttons.

Using semantic tokens everywhere means the palette can be retuned in one place.

## Extending the app

- **Add a coin/token** — add a catalog entry in `MarketDataService` (id, name, ticker,
  color, price, token flag) and drop a matching logo in `Assets/coins/`.
- **Add a screen** — create a `ViewModel` + `View`, register the `DataTemplate` in
  `App.xaml`, and add a nav item in `ShellViewModel`.
- **Change the look** — edit the tokens in `Themes/Colors.xaml` (and friends).
