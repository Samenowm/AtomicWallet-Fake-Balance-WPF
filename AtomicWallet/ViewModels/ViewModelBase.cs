using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.ViewModels;

/// <summary>Base class for page view models with a navigation hook.</summary>
public abstract class ViewModelBase : ObservableObject
{
    public virtual void OnNavigatedTo()
    {
    }
}
