namespace AtomicWallet.Models;

/// <summary>A row in the Perps market table. Fictional demo data.</summary>
public sealed class PerpMarket
{
    public string Name { get; init; } = string.Empty;       // e.g. "BTC-USD"
    public string LogoKey { get; init; } = string.Empty;    // empty → letter badge
    public string IconColor { get; init; } = "#3A6FF8";
    public string Leverage { get; init; } = string.Empty;   // e.g. "40x"
    public decimal Price { get; init; }
    public decimal Change24h { get; init; }
    public string Volume { get; init; } = string.Empty;     // pre-formatted, e.g. "$1B"
    public decimal FundingRate { get; init; }
    public string OpenInterest { get; init; } = string.Empty; // e.g. "$2.2B"
}
