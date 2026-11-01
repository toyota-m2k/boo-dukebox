using DukeBox.view.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Linq;
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
            if (ViewModel.LogMessages.Count > 0) {
                logListView.ScrollIntoView(ViewModel.LogMessages[^1]);
            }
        });
    }

    private void OnCopyAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) {
        var selected = logListView.SelectedItems.Cast<LogMessage>()
            .OrderBy(m => ViewModel.LogMessages.IndexOf(m));
        ViewModel.CopyToClipboard(selected);
        args.Handled = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        (ViewModel.LogMessages as INotifyCollectionChanged).CollectionChanged += OnLogMessagesChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        (ViewModel.LogMessages as INotifyCollectionChanged).CollectionChanged -= OnLogMessagesChanged;
    }
}
