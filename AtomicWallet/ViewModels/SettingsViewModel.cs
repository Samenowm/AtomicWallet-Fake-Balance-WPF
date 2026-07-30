using AtomicWallet.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtomicWallet.ViewModels;

/// <summary>Settings screen: Membership / Security / Private Keys tabs (UI shell).</summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly NotificationService _notifications;

    /// <summary>0 = Membership, 1 = Security, 2 = Private Keys. Defaults to Security.</summary>
    [ObservableProperty]
    private int _selectedTab = 1;

    // Set from the view's PasswordBox handlers (PasswordBox.Password isn't bindable).
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string RepeatPassword { get; set; } = string.Empty;
    public string KeysPassword { get; set; } = string.Empty;

    public SettingsViewModel(NotificationService notifications)
    {
        _notifications = notifications;
    }

    [RelayCommand] private void ShowMembership() => SelectedTab = 0;

    [RelayCommand] private void ShowSecurity() => SelectedTab = 1;

    [RelayCommand] private void ShowPrivateKeys() => SelectedTab = 2;

    [RelayCommand]
    private void ChangePassword()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            _notifications.Show("Enter a new password");
            return;
        }

        if (NewPassword.Length < 8)
        {
            _notifications.Show("Password must be at least 8 characters");
            return;
        }

        if (NewPassword != RepeatPassword)
        {
            _notifications.Show("Passwords do not match");
            return;
        }

        _notifications.Show("Password changed successfully");
    }

    [RelayCommand]
    private void ShowKeys()
    {
        _notifications.Show(string.IsNullOrWhiteSpace(KeysPassword)
            ? "Enter your password"
            : "This is a demo — no real private keys are stored");
    }
}
