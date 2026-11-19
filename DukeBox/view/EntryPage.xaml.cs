using DukeBox.DI.contract;
using DukeBox.view.viewmodel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox.view; 
public sealed partial class EntryPage : Page, INavigationAware {
    EntryPageViewModel ViewModel { get; } = App.GetService<EntryPageViewModel>();
    public EntryPage() {
        InitializeComponent();
    }

    // OnNavigatedToより先に呼ばれる。
    public void OnPageActivated() {
        ViewModel.Logger.Debug("Activated EntryPage.");
        _ = ViewModel.Prepare();
    }

    public void OnPageLeaving() {
        ViewModel.Logger.Debug("Leaving EntryPage.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        ViewModel.Logger.Debug("Navigated to EntryPage.");
        if (ViewModel.FirstTime && ViewModel.AutoStartServer.Value) {
            ViewModel.StartServerCommand.Execute();
        }
        ViewModel.FirstTime = true;
    }
}
