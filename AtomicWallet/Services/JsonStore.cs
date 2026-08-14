using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AtomicWallet.Services;

/// <summary>Persisted demo state: balances, transactions and display currency.</summary>
public sealed class SavedState
{
    public Dictionary<string, decimal> Balances { get; set; } = new();
    public List<SavedTx> Transactions { get; set; } = new();
    public string Currency { get; set; } = "USD";

    /// <summary>Ids of assets hidden from the wallet (null on legacy files = use defaults).</summary>
    public List<string>? HiddenIds { get; set; }
    public bool HideZeroBalance { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public List<SavedBuyOrder> BuyOrders { get; set; } = new();
}

public sealed class SavedBuyOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string LogoKey { get; set; } = string.Empty;
    public string IconColor { get; set; } = "#3A6FF8";
    public decimal CryptoAmount { get; set; }
    public decimal FiatAmount { get; set; }
    public string PaymentMethod { get; set; } = "Bank card";
    public string Status { get; set; } = "Completed";
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class SavedTx
{
    public string Ticker { get; set; } = string.Empty;
    public string LogoKey { get; set; } = string.Empty;
    public string IconColor { get; set; } = "#3A6FF8";
    public int Direction { get; set; }
    public decimal Amount { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>Loads/saves <see cref="SavedState"/> as JSON under %AppData%/AtomicWallet.</summary>
public static class JsonStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AtomicWallet");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "state.json");
        }
    }

    public static SavedState? Load()
    {
        try
        {
            return File.Exists(FilePath) ? JsonSerializer.Deserialize<SavedState>(File.ReadAllText(FilePath), Options) : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(SavedState state)
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(state, Options));
        }
        catch
        {
            // best-effort for a demo app
        }
    }
}
