# Design System

The visual language of the app, implemented as resource dictionaries under
`AtomicWallet/Themes/`. Everything is token-driven, so the palette and type scale can be
retuned from a single place.

## Typography

- **Primary typeface:** Roboto — weights **300 / 400 / 500 / 700**.
- **Default UI weight:** 500 (Medium); 400 (Regular) for secondary text.
- **Type scale (px):** `10 · 12 · 14 · 16 · 18 · 22 · 24 · 32`
  - `12` — captions, column headers, tickers (secondary color)
  - `14` — table cells, body text
  - `16` — emphasized labels, the wordmark
  - `18` — section / page headings
  - `24` / `32` — large amounts and hero numbers (balances, swap inputs)

UI glyph icons (navigation, arrows, actions) are vector `Path` geometries defined in
`Themes/Icons.xaml`.

## Spacing & radius

- **Corner radius:** `8px` for cards, inputs and tiles; fully rounded (`pill`) for
  primary buttons, coin circles and the total widget; `~5px` for small chips/badges.
- **Spacing scale (px):** base unit ~`4`, common values `4 · 10 · 16 · 20 · 24 · 28 ·
  40 · 60`. **`24px`** is the dominant gap and padding.

## Color tokens

Semantic tokens defined in `Themes/Colors.xaml` (each has a `Color` and a matching
`SolidColorBrush`).

| Token | Hex | Usage |
|-------|-----|-------|
| `Bg` | `#1F2843` | App / content background (dominant) |
| `RowAlt` | `#262E48` | Alternating table-row background |
| `Surface` | `#2A3552` | Cards / raised surfaces |
| `SurfaceAlt` | `#343F5C` | Inputs / hover surfaces |
| `Border` | `#404A65` | Dividers / borders |
| `TabBar` | `#2F3B55` | Top tab bar (Swap / Buy) |
| `TextPrimary` | `#FFFFFF` | Primary text, active values |
| `TextSecondary` | `#8290AD` | Labels, tickers, inactive (most common) |
| `TextFaint` | `#B8BDCC` | Faint captions |
| `AccentBlue` | `#1F8EFA` | Primary accent / active nav / links |
| `AccentCyan` | `#00C2FF` | Brand gradient end (logo, highlights) |
| `AccentPurple` | `#8785FF` | Secondary accent |
| `Positive` | `#16C784` | Positive change, success |
| `Negative` | `#F75555` | Negative change, errors, warnings |
| `Gold` | `#F1B70B` | Warnings / accents |

**Brand gradient** (logo, highlights): `#1F8EFA → #00C2FF` (blue → cyan).

## Layout

- **Window** — borderless custom chrome; the title bar shows the brand mark, the app
  name and Windows 11-style minimize / maximize-restore / close buttons.
- **Sidebar (~200px)** — brand mark + wordmark, a total-balance pill with a currency
  dropdown, then the navigation list. The active item gets a left accent bar, a
  highlighted icon and brighter text.
- **Content area** — fills the rest. Tables and forms are full-width with `24px`
  padding; numeric columns are right-aligned.
- **Table rows** — alternating `RowAlt` / `Bg` striping with a subtle hover lift.

## Component inventory

- Sidebar nav item (default / hover / active)
- Total-balance pill + currency dropdown
- Sortable table header (column label + sort-arrow indicator)
- Asset row (icon, name + chain badge, balance, value, price, 24h, portfolio, market
  cap, 7-day sparkline)
- Chain-badge pill (BASE / OP / ARB / TRX / POL …)
- Sparkline (per-trend color)
- Two-half top tab bar (Instant Swap / Order History; Buy crypto / Order History)
- Swap / amount input rows (underlined, with "Send all" and ticker)
- Pill buttons — outlined (e.g. SWAP) and filled (e.g. CONTINUE) variants
- Toggle switch (Manage assets, settings)
- Combo box + popup (currency / coin selectors)
- Toast notification
- Slim themed scrollbar
- Custom charts — sparkline, area price chart, portfolio donut

## Theme files

| File | Contents |
|------|----------|
| `Themes/Colors.xaml` | Color + brush tokens |
| `Themes/Typography.xaml` | Text styles (captions, headings, hero) |
| `Themes/Icons.xaml` | Vector glyph geometries |
| `Themes/Controls.xaml` | Button, input, switch, combo box, badge, scrollbar and caption-button styles |
