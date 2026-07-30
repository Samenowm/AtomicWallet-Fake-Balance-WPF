namespace AtomicWallet.Models;

/// <summary>A prediction-market card on the Polymarket screen. Fictional demo data.</summary>
public sealed class PolyCard
{
    public string Title { get; init; } = string.Empty;
    public string IconColor { get; init; } = "#3A6FF8";
    public string Initial { get; init; } = "?";

    /// <summary>"Yes" probability in percent (0-100); "No" is the remainder.</summary>
    public int Chance { get; init; }

    public string YesLabel { get; init; } = "Yes";
    public string NoLabel { get; init; } = "No";

    public string ChanceText => $"{Chance}% Chance";
}
