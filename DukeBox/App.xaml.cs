using DukeBox.DI.concrete;
using DukeBox.DI.contract;
using DukeBox.view;
using DukeBox.view.viewmodel;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DukeBox {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application {
        private MainWindow? _window;
        public static UtLog Logger = new UtLog("DukeBox");

        private ServiceProvider Services { get; }

        public static T GetService<T>()
            where T : class {
            if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service) {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }
            return service;
        }

        public static MainWindow MainWindow => (App.Current as App)!._window!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            InitializeComponent();
            var services = new ServiceCollection();

            // Register services here
            // services.AddSingleton<IMyService, MyService>();
            services
                .AddSingleton<IAppConfigService, AppConfigService>()
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IDBService, DBService>()
                .AddSingleton<IListingFileService, ListingFileService>()
                .AddSingleton<IHttpService, HttpService>()
                .AddSingleton<IMainThreadService, MainThreadService>()
                .AddSingleton<IPageService, PageService>()
                .AddSingleton<IDialogService, DialogService>()
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<EntryPageViewModel>()
                ;

            Services = services.BuildServiceProvider();
            UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) {
            Logger.Error(e.Exception, "Unhandled Exception");
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            _window = new MainWindow();
            _window.Activate();
            _window.AppWindow.Closing += AppWindow_Closing;
            GetService<IPageService>().Startup(_window.RootFrame);
        }

        private bool closeConfirmed = false;
        private async void ConfirmClose(Window window) {
            var r = await MessageBoxBuilder.Create(window)
                .SetMessage("Are you sure you want to exit?")
                .AddButton("Yes", id: true)
                .AddButton("No", id: false)
                .ShowAsync() as bool?;
            if (r == true) {
                closeConfirmed = true;
                window.Close();
            }
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args) {
            if (_window == null) return;
            var appConfigService = GetService<IAppConfigService>();
            if (appConfigService.NeedsConfirmOnExit && !closeConfirmed) {
                args.Cancel = true;
                ConfirmClose(_window);
            }
            GetService<IHttpService>().Stop();
        }


    }
}
