using System;

namespace AtomicWallet.Models;

/// <summary>
/// A simulated card purchase shown in Buy crypto → Order History. The fiat amount is
/// stored in USD (the entry currency); display conversion happens in the view layer.
/// </summary>
public sealed class BuyOrder
{
    public string OrderId { get; init; } = string.Empty;
    public string Ticker { get; init; } = string.Empty;
    public string LogoKey { get; init; } = string.Empty;
    public string IconColor { get; init; } = "#3A6FF8";
    public decimal CryptoAmount { get; init; }
    public decimal FiatAmount { get; init; }
    public string PaymentMethod { get; init; } = "Bank card";
    public string Status { get; init; } = "Completed";
    public DateTimeOffset Timestamp { get; init; }

    public string Title => $"Buy {Ticker}";
    public string CryptoText => $"+{CryptoAmount:0.########} {Ticker}";
    public string FiatText => $"{FiatAmount:0.00} USD";
}
