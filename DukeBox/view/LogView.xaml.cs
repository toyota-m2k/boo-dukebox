using DukeBox.view.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox.view;

public sealed partial class LogView : UserControl {
    private LogViewModel ViewModel { get; }
    public LogView() {
        ViewModel = App.GetService<LogViewModel>();
        this.InitializeComponent();
    }

    private void OnLogMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (ViewModel.StopScroll.Value) return;
        DispatcherQueue.TryEnqueue(() => {
            if (scrollViewer.ScrollableHeight > 0) {
                scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
            }
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        (ViewModel.LogMessages as INotifyCollectionChanged).CollectionChanged += OnLogMessagesChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        (ViewModel.LogMessages as INotifyCollectionChanged).CollectionChanged -= OnLogMessagesChanged;
    }
}
