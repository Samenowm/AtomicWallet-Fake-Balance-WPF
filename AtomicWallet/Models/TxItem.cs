using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Models;

public enum TxDirection
{
    Received,
    Sent
}

/// <summary>A demo transaction shown in the History screen. Fictional data.</summary>
public partial class TxItem : ObservableObject
{
    [ObservableProperty] private bool _isExpanded;

    public string Ticker { get; init; } = string.Empty;
    public string LogoKey { get; init; } = string.Empty;
    public string IconColor { get; init; } = "#3A6FF8";
    public TxDirection Direction { get; init; }
    public decimal Amount { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Hash { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }

    public string DirectionLabel => Direction == TxDirection.Received ? "From" : "To";

    public string SignedAmount =>
        $"{(Direction == TxDirection.Received ? "+" : "-")}{Amount:0.######}";

    public bool IsReceived => Direction == TxDirection.Received;
}
