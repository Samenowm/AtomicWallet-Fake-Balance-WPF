using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Services;

/// <summary>Lightweight in-app toast. The shell binds to Message / IsVisible.</summary>
public sealed partial class NotificationService : ObservableObject
{
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isVisible;

    /// <summary>When false, <see cref="Show"/> is suppressed (Manage assets toggle).</summary>
    public bool Enabled { get; set; } = true;

    public NotificationService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            IsVisible = false;
        };
    }

    public void Show(string message)
    {
        if (!Enabled)
        {
            return;
        }

        Message = message;
        IsVisible = true;
        _timer.Stop();
        _timer.Start();
    }
}
