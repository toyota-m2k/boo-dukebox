using DukeBox.view.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox; 

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window {
    public Frame RootFrame => _rootFrame;
    private MainWindowViewModel ViewModel { get; }

    public MainWindow() {
        ViewModel = App.GetService<MainWindowViewModel>();
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        ViewModel.WindowTitle.Subscribe(title => {
            this.Title = title;
        });
    }
}
