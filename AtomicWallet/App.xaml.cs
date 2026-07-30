using System.Windows;
using AtomicWallet.Services;
using AtomicWallet.ViewModels;
using AtomicWallet.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AtomicWallet;

public partial class App : Application
{
    private ServiceProvider? _services;
    private MarketDataService? _market;
    private PriceFeedService? _priceFeed;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddSingleton<MarketDataService>();
        services.AddSingleton<PriceFeedService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();

        // Shared display-currency instance, reachable from XAML as {StaticResource Fx}.
        Resources["Fx"] = Services.Fx.Instance;

        _market = _services.GetRequiredService<MarketDataService>();
        _market.Start();

        // Start pulling real spot prices from the live feed (no-op if offline).
        _priceFeed = _services.GetRequiredService<PriceFeedService>();
        _priceFeed.Start();

        // Apply the persisted notification preference to the live toast service.
        _services.GetRequiredService<NotificationService>().Enabled = _market.NotificationsEnabled;

        var window = _services.GetRequiredService<MainWindow>();
        window.DataContext = _services.GetRequiredService<ShellViewModel>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _market?.Save();
        _priceFeed?.Dispose();
        _market?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
