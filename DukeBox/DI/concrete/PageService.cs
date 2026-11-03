using DukeBox.DI.contract;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace DukeBox.DI.concrete; 
public class PageService : IPageService {
    private Page? _currentPage = null;
    private Frame? _rootFrame = null;
    private ReactivePropertySlim<Type?> _currentPageType = new(null);
    public ReadOnlyReactivePropertySlim<bool> CanGoBack { get; }

    public Page? CurrentPage => _currentPage;
    public XamlRoot XamlRoot {
        get {
            if (_rootFrame == null) {
                throw new InvalidOperationException("Root frame is not set. Call Startup() first.");
            }
            return _rootFrame.XamlRoot;
        }
    }

    public PageService() {
        CanGoBack = _currentPageType
            .Select(x => x != null && x != typeof(view.EntryPage))
            .ToReadOnlyReactivePropertySlim();
    }

    public void GoBack() {
        if (CanGoBack.Value) {
            // ２ページしかないので、戻るなら、必ずEntryPage
            GoToEntryPage();
        }
    }

    public void GoToEntryPage() {
        NavigateTo(typeof(view.EntryPage));
    }

    public void GoToServerPage() {
        NavigateTo(typeof(view.ServerPage));
    }

    public void Startup(Frame rootFrame) {
        _rootFrame = rootFrame;
        rootFrame.Navigated += OnNavigated;
        GoToEntryPage();
    }

    private void OnNavigated(object sender, NavigationEventArgs e) {
        if (sender is Frame frame) {
            // BackStackは自力で管理する。
            frame.BackStack.Clear();
            if (e.Content is Page page) {
                _currentPage = page;
                if (page is INavigationAware after) {
                    after.OnPageActivated();
                }
            }
        }
    }

    private void NavigateTo(Type pageType) {
        if (_rootFrame == null) {
            throw new InvalidOperationException("Root frame is not set. Call Startup() first.");
        }
        if (_currentPageType.Value != pageType) {
            _currentPageType.Value = pageType;
            if (_currentPage is INavigationAware before) {
                before.OnPageLeaving();
            }
            _rootFrame?.Navigate(pageType);
        }
    }

}
