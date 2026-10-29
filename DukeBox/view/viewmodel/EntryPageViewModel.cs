using DukeBox.DB;
using DukeBox.DI.contract;
using DukeBox.model;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DukeBox.view.viewmodel;

public class EntryPageViewModel {
    public UtLog Logger = new UtLog("EntryPage");

    #region Media Source / Scanner
    public ReactiveProperty<long> ScanIntervalMs { get; } = new(0);

    public ObservableCollection<ICategory> Channels { get; } = new();
    public ObservableCollection<ISourceDirectory> PathInChannel { get; } = new();
    public ReactiveProperty<ICategory?> SelectedChannel { get; } = new();
    public ReactiveProperty<ISourceDirectory?> SelectedPath { get; } = new();

    public ReactiveCommand AddChannelCommand { get; } = new();
    public ReactiveCommand RemoveChannelCommand { get; } = new();
    public ReactiveCommand AddPathCommand { get; } = new();
    public ReactiveCommand RemovePathCommand { get; } = new();


    #endregion

    #region Conversion / FFMpeg


    // FFMpeg
    public ReactiveProperty<bool> UseFFMpeg { get; } = new(false);
    public ReactiveProperty<string> FFMpegPath { get; } = new(string.Empty);
    public ReactiveProperty<string> WorkingPath { get; } = new(string.Empty);
    // Converted Source
    public ReactiveProperty<string> ConvertedSourcePath { get; } = new(string.Empty);

    #endregion


    #region Server

    public ReactivePropertySlim<bool> EnableHttp { get; } = new(false);
    public ReactiveProperty<bool> EnableSSL { get; } = new(false);
    public ReactiveProperty<string> ServerName { get; } = new(string.Empty);
    public ReactiveProperty<int> ServerPort { get; } = new(0);
    public ReactiveProperty<int> SSLServerPort { get; } = new(0);
    public ReactiveProperty<bool> EnableMDNSAdvertising { get; } = new(false);
    public ReactiveProperty<string> PFXFilePath { get; } = new(string.Empty);
    public ReactiveProperty<string> PFXPassword { get; } = new(string.Empty);

    public ReactiveCommand BrowsePfxFileCommand { get; } = new();
    public ReactiveCommand GeneratePfxFileCommand { get; } = new();

    #endregion

    #region Status

    public ReactiveProperty<bool> Busy { get; } = new(false);
    public ReactiveProperty<string> BusyMessage { get; } = new("");
    public ReadOnlyReactiveProperty<bool> IsBusyMessageVisible { get; }
    public Task WithBusy(string message, Func<Task> action) {
        var orgBusy = Busy.Value;
        var orgMessage = BusyMessage.Value;
        BusyMessage.Value = message;
        Busy.Value = true;
        try {
            return action();
        }
        finally {
            Busy.Value = orgBusy;
            BusyMessage.Value = orgMessage;
        }
    }
    public Task<T> WithBusy<T>(string message, Func<Task<T>> action) {
        var orgBusy = Busy.Value;
        var orgMessage = BusyMessage.Value;
        BusyMessage.Value = message;
        Busy.Value = true;
        try {
            return action();
        }
        finally {
            Busy.Value = orgBusy;
            BusyMessage.Value = orgMessage;
        }
    }

    public ReactiveCommand StartServerCommand { get; } = new();

    #endregion

    private ISettingsService _settingsService;
    private IDBService _dBService;
    private IListingFileService _listingFileService;
    private IHttpService _httpService;
    private IDialogService _dialogService;
    private IPageService _pageService;


    public EntryPageViewModel(
        ISettingsService settingsService, 
        IDBService dBService,
        IListingFileService listingFileService,
        IHttpService httpService,
        IDialogService dialogService,
        IPageService pageService) {
        _settingsService = settingsService;
        _dBService = dBService;
        _listingFileService = listingFileService;
        _httpService = httpService;
        _dialogService = dialogService;
        _pageService = pageService;

        // Channels
        Channels.Clear();
        Channels.Add(new CategoryEntry() { Id = 0, Name = "Default" });
        dBService.CategoryStore.List().Aggregate(Channels, (acc, cat) => {
            acc.Add(cat);
            return acc;
        });
        SelectedChannel.Subscribe(OnChannelChanged);
        SelectedChannel.Value = Channels[0];

        // media source
        ScanIntervalMs.Value = (long)_settingsService.ScanInterval.TotalMilliseconds;

        // conversion
        UseFFMpeg.Value = _settingsService.UseFFMpeg;
        FFMpegPath.Value = _settingsService.FFMpegPath;
        WorkingPath.Value = _settingsService.WorkingPath;
        ConvertedSourcePath.Value = _settingsService.ConvertedSourcePath;

        // server
        EnableHttp.Value = _settingsService.EnableHttp;
        EnableSSL.Value = _settingsService.EnableSSL;
        ServerName.Value = _settingsService.ServerName;
        ServerPort.Value = _settingsService.ServerPort;
        SSLServerPort.Value = _settingsService.SSLServerPort;
        EnableMDNSAdvertising.Value = _settingsService.EnableMDNSAdvertising;
        PFXFilePath.Value = _settingsService.PFXFilePath;
        PFXPassword.Value = _settingsService.PFXPassword;

        // commands
        AddChannelCommand.Subscribe(AddChannel);
        RemoveChannelCommand.Subscribe(RemoveChannel);
        AddPathCommand.Subscribe(AddPath);
        RemovePathCommand.Subscribe(RemovePath);

        BrowsePfxFileCommand.Subscribe(BrowsePfxFile);
        GeneratePfxFileCommand.Subscribe(GeneratePfxFile);

        // Status
        IsBusyMessageVisible = BusyMessage.Select(it => !string.IsNullOrWhiteSpace(it)).ToReadOnlyReactiveProperty();
        StartServerCommand.Subscribe(StartServer);
    }

    private void OnChannelChanged(ICategory? newChannel) {
        PathInChannel.Clear();
        if (newChannel != null) {
            var paths = _dBService.SourceDirectoryStore.List().Where(it => it.Category == newChannel.Id || (it.Category == null && newChannel.Id == 0)).ToList();
            foreach (var path in paths) {
                PathInChannel.Add(path);
            }
        }
    }

    private bool _dirty = true;     // 初回は必ず UpdateScanners()を実行する

    private bool IsSameCategory(ICategory? c1, ICategory? c2) {
        return IsSameCategory(c1?.Id, c2?.Id);
    }
    private bool IsSameCategory(long? c1, long? c2) {
        var id1 = c1 ?? 0;
        var id2 = c2 ?? 0;
        return id1 == id2;
    }

    private async void AddChannel() {
        var name = await _dialogService.ShowInputText("Add Channel", "Enter channel name:");
        if (string.IsNullOrWhiteSpace(name)) {
            return;
        }
        if (name == Channels[0].Name) {
            await _dialogService.ShowMessage("Channel", "Default channel is reserved.");
            return;
        }
        if (null != _dBService.CategoryStore.GetByName(name)) {
            await _dialogService.ShowMessage("Channel", "Channel with the same name already exists.");
            return;
        }

        await WithBusy("Adding channel...", async () => {
            await _dBService.EditCategoryStore(async store => {
                var newCategory = await Task.Run(() => store.Add(name));
                if (newCategory == null) {
                    return false;
                }
                Channels.Add(newCategory);
                SelectedChannel.Value = newCategory;
                _dirty = true;
                return true;
            });
        });
    }

    private async void RemoveChannel() {
        var channel = SelectedChannel.Value;
        if (channel == null) {
            return;
        }
        if (channel.Id == 0) {
            await _dialogService.ShowMessage("Channel", "Default channel cannot be removed.");
            return;
        }

        if (await _dialogService.ShowYesNo(
            "Remove Channel",
            $"Are you sure you want to remove channel '{channel.Name}'?\r\n" +
            $"This will also remove all paths and media files in this channel.")) {

            await WithBusy("Removing channel...", async () => {
                await Task.Run(async ()=>_dBService.RemoveCategory((CategoryEntry)channel));
                Channels.Remove(channel);
                SelectedChannel.Value = Channels[0];
                _dirty = true;
            });
        }
    }

    private async Task<bool> CheckPathConfliction(string path, bool recursive) {
        return await WithBusy("Checking path confliction...", async () => {
            var conflict = _dBService.SourceDirectoryStore.CheckConfliction(path, recursive);
            if (conflict != ConflictType.None) {
                string message;
                switch (conflict) {
                    case ConflictType.Registered:
                        message = $"The path '{path}' is already registered.";
                        break;
                    case ConflictType.SubdirectoryOfRegistered:
                        message = $"The path '{path}' is a subdirectory of a registered source directory.";
                        break;
                    case ConflictType.ParentDirectoryOfRegistered:
                        message = $"The path '{path}' is a parent directory of a registered source directory.";
                        break;
                    default:
                        message = $"unexpected error";
                        break;
                }
                await _dialogService.ShowMessage("Source Folder", message);
                return false;
            }
            return true;
        });
    }

    private async void AddPath() {
        var currentChannel = SelectedChannel.Value;
        if (currentChannel == null) {
            await _dialogService.ShowMessage("Source Folder", "Please select a channel first.");
            return;
        }
        var dstFolder = await FolderPickerBuilder.Create(App.MainWindow)
            .SetIdentifier("Source Folder")
            .SetViewMode(Windows.Storage.Pickers.PickerViewMode.List)
            .PickAsync();
        if (dstFolder == null) {
            return;
        }
        var path = dstFolder.Path;
        if (!await CheckPathConfliction(path, false)) {
            return;
        }
        var checkBox = new CheckBox() {
            Content = "Scan subdirectories recursively",
            IsChecked = false,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 10, 0, 0)
        };
        var result = await _dialogService.Builder()
            .Title("Source Folder")
            .Content(checkBox)
            .ShowOkCancel();
        if (!result) {
            return;
        }
        var recursive = checkBox.IsChecked ?? false;
        if (recursive && !await CheckPathConfliction(path, true)) {
            return;
        }

        await WithBusy("Adding source folder...", async () => {
            await _dBService.EditSourceDirectoryStore(async store => {
                var newEntry = await Task.Run(()=>store.Add(path, recursive, currentChannel.Id));
                if (newEntry == null) {
                    return false;
                }
                PathInChannel.Add(newEntry);
                SelectedPath.Value = newEntry;
                _dirty = true;
                return true;
            });
        });
    }
    private async void RemovePath() {
        var pathEntry = SelectedPath.Value;
        if (pathEntry == null) {
            return;
        }
        
        if (!IsSameCategory(pathEntry.Category, SelectedChannel.Value?.Id)) {
            Logger.Error("selected path is not in current category.");
            return;
        }
        var index = PathInChannel.IndexOf(pathEntry);
        if (await _dialogService.ShowYesNo(
            "Remove Source Folder",
            $"Are you sure you want to remove source folder '{pathEntry.Path}'?\r\n" +
            $"This will also remove all media files in this folder from the database.")) {
            await WithBusy("Removing source folder...", async () => {
                await Task.Run(async () => _dBService.RemoveSourceDirectory(pathEntry));
                PathInChannel.Remove(pathEntry);
                if (PathInChannel.Count > 0) {
                    if (index >= PathInChannel.Count) {
                        index = PathInChannel.Count - 1;
                    }
                    SelectedPath.Value = PathInChannel[index];
                }
                else {
                    SelectedPath.Value = null;
                }
                _dirty = true;
            });
        }
    }

    private async void BrowsePfxFile() {
        var file = await FileOpenPickerBuilder
            .Create(App.MainWindow)
            .AddExtension(".pfx")
            .Build()
            .PickSingleFileAsync();

        if (file!=null) {
            PFXFilePath.Value = file.Path;
        }
    }
    private async void GeneratePfxFile() {
        var dlg = new GenerateCertDialog {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            InitialPfxPath = PFXFilePath.Value,
            InitialPassword = PFXPassword.Value,
            InitialSubject = string.IsNullOrWhiteSpace(ServerName.Value)
                ? $"SecureArchive-{Environment.MachineName}"
                : ServerName.Value,
        };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary && dlg.Result != null) {
            PFXFilePath.Value = dlg.Result.PfxPath;
            PFXPassword.Value = dlg.Result.Password;
        }
    }

    public async Task Prepare() {
        await WithBusy("Stopping Servers...", async () => {
            // サーバーを停止する。
            await _httpService.StopAsync();
            // スキャナを停止する。
            await _listingFileService.StopScanAsync();
        });
    }

    /**
     * 設定を保存する。
     * 設定が変更されていた場合、スキャナの更新も行う。
     * ServerPageを表示（＝サーバーを起動）する前に実行する。
     */
    private async Task<bool> SaveAndCheck() {
        _settingsService.ScanInterval = TimeSpan.FromMilliseconds(ScanIntervalMs.Value);
        _settingsService.UseFFMpeg = UseFFMpeg.Value;
        _settingsService.FFMpegPath = FFMpegPath.Value;
        _settingsService.WorkingPath = WorkingPath.Value;
        _settingsService.ConvertedSourcePath = ConvertedSourcePath.Value;
        _settingsService.EnableHttp = EnableHttp.Value;
        _settingsService.EnableSSL = EnableSSL.Value;
        _settingsService.ServerName = ServerName.Value;
        _settingsService.ServerPort = ServerPort.Value;
        _settingsService.SSLServerPort = SSLServerPort.Value;
        _settingsService.EnableMDNSAdvertising = EnableMDNSAdvertising.Value;
        _settingsService.PFXFilePath = PFXFilePath.Value;
        _settingsService.PFXPassword = PFXPassword.Value;
        _settingsService.Save();

        if (_dirty) {
            await WithBusy("Updating Scanners...", async () => {
                await _listingFileService.UpdateScanners();
            });
            _dirty = false;
        }
        if (!EnableHttp.Value&&!EnableSSL.Value) {
            await _dialogService.ShowMessage("Server", "HTTP server is not enabled.");
            return false;
        }
        return true;
    }

    private async void StartServer() {
        await WithBusy("Checking settings...", async () => {
            if (!await SaveAndCheck()) {
                return;
            }
            _listingFileService.StartScan();

            _httpService.Start();
            _pageService.GoToServerPage();
        });
    }
}
