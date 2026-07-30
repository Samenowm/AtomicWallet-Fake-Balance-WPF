using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.ViewModels;

/// <summary>A sidebar navigation entry.</summary>
public partial class NavItem : ObservableObject
{
    [ObservableProperty]
    private bool _isActive;

    public NavItem(string title, string iconKey)
    {
        Title = title;
        IconKey = iconKey;
    }

    public string Title { get; }

    /// <summary>Key of the geometry in Themes/Icons.xaml.</summary>
    public string IconKey { get; }
}
