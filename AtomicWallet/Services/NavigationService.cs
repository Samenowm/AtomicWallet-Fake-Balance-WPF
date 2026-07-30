using System.Collections.Generic;
using AtomicWallet.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtomicWallet.Services;

/// <summary>
/// Drives the shell content area. Top-level (sidebar) navigation resets the back
/// stack; in-page navigation (coin → detail → send/receive) pushes onto it.
/// </summary>
public sealed partial class NavigationService : ObservableObject
{
    private readonly Stack<ViewModelBase> _back = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    private ViewModelBase? _currentPage;

    public bool CanGoBack => _back.Count > 0;

    public void Navigate(ViewModelBase page, bool resetRoot = false)
    {
        if (resetRoot)
        {
            _back.Clear();
        }
        else if (CurrentPage != null)
        {
            _back.Push(CurrentPage);
        }

        CurrentPage = page;
        OnPropertyChanged(nameof(CanGoBack));
        page.OnNavigatedTo();
    }

    public void GoBack()
    {
        if (_back.Count == 0)
        {
            return;
        }

        CurrentPage = _back.Pop();
        OnPropertyChanged(nameof(CanGoBack));
    }
}
