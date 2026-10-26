using DukeBox.DI.contract;
using DukeBox.model;
using DukeBox.scanner;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace DukeBox.DI.concrete; 
public class ListingFileService {
    UtLog Logger = new UtLog("ListingFileService");
    IDBService DBService;
    ISettingsService SettingService;
    List<FileScanner> Scanners = new List<FileScanner>();
    ObservableCollection<IMediaFile> MediaFiles;


    public ListingFileService(ISettingsService settingService, IDBService dBService) {
        this.DBService = dBService;
        this.SettingService = settingService;
        this.TargetPathList = DBService.
        // Settingsに登録されているTargetPathListに対してFileScanner を構築する
        foreach (var path in TargetPathList) {
            var scanner = new FileScanner(path);
            // DBに登録されているファイル一覧
            var files = DBService.MediaFileStore.ListAs<string>(
                predicate: rec => rec.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase),
                select: rec => rec.Path);
            // DBの登録を元に、FileScannerの内容を復元し、存在しないファイルをDBから削除する。
            var removed = scanner.RestoreWith(files);
            if (removed.Count > 0) {
                // 存在しないファイルがあった場合は、DBから削除する。
                Logger.Info($"{removed.Count} files removed from DB.");
                DBService.EditMediaFileStore(store => {
                    var removedEntries = store.List(it => removed.Any(path => string.Equals(path, it.Path, StringComparison.OrdinalIgnoreCase)));
                    store.Remove(removedEntries);
                    return true;    // updated
                });
            }
            Scanners.Add(scanner);
        }
        MediaFiles = new ObservableCollection<IMediaFile>(DBService.MediaFileStore.List());
        DBService.MediaFileStore.Changes.Subscribe(OnMediaFileStoreChanged);
    }

    public async void Scan() {
        foreach (var scanner in Scanners) {
            var (appended, removed)= scanner.Scan();
            if (appended.Count > 0) {
                Logger.Info($"{appended.Count} new files found in {scanner.DirectoryPath}");
                await DBService.EditMediaFileStore(async store => {
                    foreach (var file in appended) {
                        var duration = (await MediaUtil.GetDurationAsync(file.FullName))?.TotalMilliseconds ?? 0;
                        store.AddOrUpdate(file.FullName, file.Name, file.Length, file.Extension, (long)Math.Round(duration));
                    }
                    return true;    // updated
                });
            }
        }
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
                foreach (var item in DBService.MediaFileStore.List()) {
                    MediaFiles.Add(item);
                }
                break;
        }
    }

}
