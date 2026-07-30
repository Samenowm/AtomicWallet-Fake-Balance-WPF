namespace AtomicWallet.ViewModels;

/// <summary>Temporary page shown for screens not yet implemented.</summary>
public sealed class PlaceholderViewModel : ViewModelBase
{
    public PlaceholderViewModel(string title)
    {
        Title = title;
    }

    public string Title { get; }
}
