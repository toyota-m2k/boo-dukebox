using DukeBox.DI.contract;
using DukeBox.view.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox.view; 
public sealed partial class EntryPage : Page, INavigationAware {
    EntryPageViewModel ViewModel { get; } = App.GetService<EntryPageViewModel>();
    public EntryPage() {
        InitializeComponent();
    }

    public void OnPageActivated() {
        ViewModel.Logger.Debug("Activated EntryPage.");
        _ = ViewModel.Prepare();
    }

    public void OnPageLeaving() {
        ViewModel.Logger.Debug("Leaving EntryPage.");
    }
}
