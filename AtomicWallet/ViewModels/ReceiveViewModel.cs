using System.IO;
using System.Windows.Media.Imaging;
using AtomicWallet.Models;
using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;

namespace AtomicWallet.ViewModels;

/// <summary>Receive screen: shows the asset's (demo) address and a QR code.</summary>
public sealed partial class ReceiveViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly NotificationService _notifications;

    public ReceiveViewModel(Asset coin, NavigationService navigation, NotificationService notifications)
    {
        Coin = coin;
        _navigation = navigation;
        _notifications = notifications;
        Address = DemoAddress(coin.Ticker);
        QrImage = GenerateQr(Address);
    }

    public Asset Coin { get; }

    public string Address { get; }

    public BitmapSource QrImage { get; }

    [RelayCommand]
    private void Copy()
    {
        try { System.Windows.Clipboard.SetText(Address); }
        catch { /* clipboard may be locked */ }
        _notifications.Show("Address copied to clipboard");
    }

    [RelayCommand]
    private void Close() => _navigation.GoBack();

    // A fixed, obviously-demo address. Not a real key or wallet.
    private static string DemoAddress(string ticker) => ticker switch
    {
        "BTC" => "1PRE8gvvJLpbahMFHQ2xXThTXtv4hqd1q4",
        _ => "0xDEMO0000ExampleAddressOnly00000000000000"
    };

    private static BitmapSource GenerateQr(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(20);

        var img = new BitmapImage();
        using var ms = new MemoryStream(png);
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }
}
