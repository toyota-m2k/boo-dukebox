using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DukeBox.DI.contract;

public interface INavigationAware {
    void OnPageActivated();
    void OnPageLeaving();
}

public interface IPageService {
    ReadOnlyReactivePropertySlim<bool> CanGoBack { get; }
    void Startup(Frame rootFrame);
    void GoBack();
    Page? CurrentPage { get; }
    XamlRoot XamlRoot { get; }

    void GoToEntryPage();
    void GoToServerPage();

}
