using DukeBox.DI.concrete;
using DukeBox.DI.contract;
using DukeBox.view;
using DukeBox.view.viewmodel;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

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
                .AddSingleton<LogViewModel>()
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<EntryPageViewModel>()
                .AddSingleton<ServerPageViewModel>()
                ;

            Services = services.BuildServiceProvider();
            UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) {
            Logger.Error(e.Exception, "Unhandled Exception");
            if (e.Exception is ArgumentOutOfRangeException { Source: "WinUI.TableView" }) {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            _window = new MainWindow();
            _window.Activate();
            _window.AppWindow.Closing += AppWindow_Closing;
            UtLog.GlobalLoggerChain.AddLogger(GetService<LogViewModel>());
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
        private bool serverStopping = false;
        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args) {
            if (_window == null) return;
            var appConfigService = GetService<IAppConfigService>();
            if (appConfigService.NeedsConfirmOnExit && !closeConfirmed) {
                args.Cancel = true;
                ConfirmClose(_window);
                return;
            }

            // HTTPサーバー起動中なら停止してから終了
            var httpService = GetService<IHttpService>();
            if (!serverStopping && httpService.Running.Value) {
                args.Cancel = true;
                Task.Run(async () => {
                    await httpService.StopAsync();
                    GetService<IMainThreadService>().Run(() => {
                        if (_window == null) return;
                        _window.AppWindow.Closing -= AppWindow_Closing;
                        _window.Close();
                    });
                    // StopAsyncがタイムアウトした場合、Running.Value が false にならないので、
                    // チェックをスキップするためのフラグを立てて、再度 Close() を呼ぶ。
                    serverStopping = true;
                });
            }
        }


    }
}
