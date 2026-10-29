using DukeBox.DI.contract;
using DukeBox.model;
using DukeBox.scanner;
using io.github.toyota32k.BooLib.Utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.WebUI;

namespace DukeBox.DI.concrete; 
public class ListingFileService : IListingFileService {
    UtLog Logger = new UtLog("ListingFileService");
    IDBService DBService;
    ISettingsService SettingService;
    List<FileScanner> Scanners = new List<FileScanner>();
    public ReactiveProperty<bool> Ready { get; } = new(false);
    public ReactiveProperty<bool> IsRunning { get; } = new(false);
    public ObservableCollection<IMediaFile> MediaFiles { get; }
    
    IReadOnlyReactiveProperty<bool> IListingFileService.IsRunning => IsRunning;

    #region Scan の排他制御用

    private SemaphoreSlim lockSemaphoe = new SemaphoreSlim(1, 1);
    private AtomicBoolean busy = new AtomicBoolean(false);

    #endregion

    public ListingFileService(ISettingsService settingService, IDBService dBService) {
        this.DBService = dBService;
        this.SettingService = settingService;
        MediaFiles = new ObservableCollection<IMediaFile>(DBService.MediaFileStore.Enumerable());
        DBService.MediaFileStore.Changes.Subscribe(OnMediaFileStoreChanged);
    }


    public async Task UpdateScanners() {
        await StopScanAsync();
        await lockSemaphoe.WaitAsync();
        try {
            Scanners.Clear();
            await Task.Run(() => {
                var pathList = DBService.SourceDirectoryStore.List().ToList().Select(it => it.Path);
                // Settingsに登録されているTargetPathListに対してFileScanner を構築する
                foreach (var path in pathList) {
                    var scanner = new FileScanner(path);
                    // DBに登録されているファイル一覧
                    var files = DBService.MediaFileStore.List()
                                    .Where(rec => rec.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                                    .Select(rec => rec.Path);
                    // DBの登録を元に、FileScannerの内容を復元し、存在しないファイルをDBから削除する。
                    var removed = scanner.RestoreWith(files);
                    if (removed.Count > 0) {
                        // ファイルが存在しないエントリは、DBから削除する。
                        Logger.Info($"{removed.Count} files removed from DB.");
                        var removedEntries = DBService.MediaFileStore.List()
                            .Where(it => removed.Any(path => string.Equals(path, it.Path, StringComparison.OrdinalIgnoreCase)));
                        DBService.RemoveMediaFiles(removedEntries);
                    }
                    Scanners.Add(scanner);
                }
            });
        } finally {
            lockSemaphoe.Release();
        }
    }

    //private async Task CancelScan(bool keepLock=true) {
    //    await StopScanAsync();
    //   cancelling = true;
    //    Logger.Warn("Cancelling scan...");
    //    await lockSemaphoe.WaitAsync();
    //    cancelling = false;
    //    if (!keepLock) {
    //        lockSemaphoe.Release();
    //    }
    //}

    private async Task ScanNow(CancellationToken ct) {
        await Task.Run(async () => {
            if (!busy.CompareAndSet(false, true)) {
                Logger.Warn("Scan already in progress.");
                return;
            }
            try {
                await lockSemaphoe.WaitAsync();
                foreach (var scanner in Scanners) {
                    if (ct.IsCancellationRequested) { 
                        break;
                    }
                    var (appended, removed) = scanner.Scan();
                    if (appended.Count > 0) {
                        Logger.Info($"{appended.Count} new files found in {scanner.DirectoryPath}");
                        await DBService.EditMediaFileStore(async store => {
                            foreach (var file in appended) {
                                var duration = (await MediaUtil.GetDurationAsync(file.FullName))?.TotalMilliseconds ?? 0;
                                store.AddOrUpdate(file.FullName, file.Name, file.Length, file.Extension, (long)Math.Round(duration));
                                if (ct.IsCancellationRequested) {
                                    Logger.Warn("Scan cancelled.");
                                    break;
                                }
                            }
                            return true;    // updated
                        });
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "Error during scanning.");
            }
            finally {
                busy.Value = false;
                lockSemaphoe.Release();
            }
        });
    }

    private void OnMediaFileStoreChanged(DataChangeInfo change) {

        switch (change.Type) {
            case DataChangeInfo.Change.Add:
                MediaFiles.Add(change.Item);
                break;
            case DataChangeInfo.Change.Update:
                var index = MediaFiles.IndexOf(change.Item);
                if (index >= 0) {
                    MediaFiles[index] = change.Item;
                }
                break;
            case DataChangeInfo.Change.Remove:
                MediaFiles.Remove(change.Item);
                break;
            case DataChangeInfo.Change.ResetAll:
                MediaFiles.Clear();
                foreach (var item in DBService.MediaFileStore.Enumerable()) {
                    MediaFiles.Add(item);
                }
                break;
        }
    }
    private CancellationTokenSource? cancellationTokenSource = null;
    public void StartScan() {
        var interval = SettingService.ScanInterval;
        if (IsRunning.Value) {
            Logger.Error("Scan already active.");
            return;
        }
        IsRunning.Value = true;
        var cts = new CancellationTokenSource();
        cancellationTokenSource = cts;
        Task.Run(async () => {
            try {
                while (!cts.IsCancellationRequested) {
                    await ScanNow(cts.Token);
                    await Task.Delay(interval, cts.Token);
                }
            }
            finally {
                IsRunning.Value = false;
            }
        });
    }

    public async Task StopScanAsync() {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = null;
        try {
            await IsRunning.FirstAsync(it => !it).Timeout(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException) {
            Logger.Warn("StopScanAsync: Timeout waiting for scan to stop.");
        }
    }
}
