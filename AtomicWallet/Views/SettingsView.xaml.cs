using System.Windows;
using System.Windows.Controls;
using AtomicWallet.ViewModels;

namespace AtomicWallet.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private SettingsViewModel? Vm => DataContext as SettingsViewModel;

    private static void SyncPlaceholder(PasswordBox box, UIElement placeholder)
        => placeholder.Visibility = string.IsNullOrEmpty(box.Password) ? Visibility.Visible : Visibility.Collapsed;

    private void OldPwBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.OldPassword = OldPwBox.Password;
        SyncPlaceholder(OldPwBox, OldPwPh);
    }

    private void NewPwBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.NewPassword = NewPwBox.Password;
        SyncPlaceholder(NewPwBox, NewPwPh);
    }

    private void RepeatPwBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.RepeatPassword = RepeatPwBox.Password;
        SyncPlaceholder(RepeatPwBox, RepeatPwPh);
    }

    private void KeysPwBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Vm != null) Vm.KeysPassword = KeysPwBox.Password;
        SyncPlaceholder(KeysPwBox, KeysPwPh);
    }
}
