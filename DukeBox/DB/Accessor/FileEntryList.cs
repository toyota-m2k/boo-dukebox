using DukeBox.DI.contract;
using DukeBox.model;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Windows.ApplicationModel.Store;

namespace DukeBox.DB.Accessor;

class ChangeInfoQueue : IDataChangeEventSource {
    private DBConnector _connector; // DBConnectorは必要ないが、lockのために保持しておく
    private Subject<DataChangeInfo> _changes = new();
    private List<DataChangeInfo> _queue = new List<DataChangeInfo>();
    bool resetAll = false;

    public ChangeInfoQueue(DBConnector connector) {
        _connector = connector;
    }

    public void Enqueue(DataChangeInfo info) {
        lock (_connector) {
            if (info.Type == DataChangeInfo.Change.ResetAll) {
                resetAll = true;
                _queue.Clear();
                return;
            }
            else if (resetAll) {
                // ResetAllが来た後は、Add/Update/Removeは無視する
                return;
            }
            else if (info.Type == DataChangeInfo.Change.Remove) {
                _queue.RemoveAll(it => it.Item.Id == info.Item.Id);
            }
            _queue.Add(info);
        }
    }
    public IObservable<DataChangeInfo> Changes => _changes;

    public void Submit() {
        var all = false;
        List<DataChangeInfo> queue;
        lock (_connector) {
            if (_queue.Count == 0 && !resetAll) {
                return; // 変更がない場合は何もしない
            }
            all = resetAll;
            queue = _queue.ToList(); // キューをコピーしてからクリアする
            _queue.Clear(); // キューをクリア
            resetAll = false; // ResetAllフラグをクリア
        }
        
        // 実際の通知は lock外で行う
        if (all) {
            _changes.OnNext(DataChangeInfo.ResetAll());
            resetAll = false;
        }
        else {
            foreach (var info in queue) {
                _changes.OnNext(info);
            }
        }
    }

    public void Reset() {
        lock (_connector) {
            _queue.Clear();
            resetAll = false; // ResetAllフラグをクリア
        }
    }
}


public class FileEntryList : IMutableMediaFileStore {
    private DBConnector _connector;
    private DbSet<FileEntry> _entries;

    private ChangeInfoQueue _changeInfoQueue;
    public IDataChangeEventSource ChangeEventSource => _changeInfoQueue;

    public IObservable<DataChangeInfo> Changes => _changeInfoQueue.Changes;

    //public IObservable<DataChangeInfo> Changes => _changes;

    public FileEntryList(DBConnector connector) {
        _connector = connector;
        _entries = connector.Entries;
        _changeInfoQueue = new ChangeInfoQueue(_connector);
    }

    //public IEnumerable<IMediaFile> RawList() {
    //    return _entries;
    //}

    public IEnumerable<IMediaFile> Enumerable(bool tracking = false) {
        return _entries.Include(e => e.CategoryRef).Let(it => {
            if (tracking) {
                return it;
            } else { 
                return it.AsNoTracking();
            }
        });
    }
    public IList<IMediaFile> List(bool tracking = false) {
        return Enumerable(tracking).ToList();
    }

    public IMediaFile AddOrUpdate(string path, string name, long size, string type, long duration = 0L, bool converted = false,
        int rating = 0, int mark = 0, string label = "", long category = 0, string chapters = "") {
        var entry = GetByPath(path);
        if (entry != null) {
            return Update(entry, name, size, duration, converted, rating, mark, label, category, chapters);
        } else {
            return Add(path, name, size, type, duration, converted, rating, mark, label, category, chapters);
        }
    }
    
    public IMediaFile Add(string path, string name, long size, string type, long duration=0L, bool converted=false,
        int rating=0, int mark=0, string label="", long category=0, string chapters="") {
        if(GetByPath(path) != null) {
            throw new ArgumentException($"already exists: {path}");
        }
        var typeValue = type;
        if(type.StartsWith(".")) {
            typeValue = type.Substring(1);
        }
        var info = new FileInfo(path);
        if (!info.Exists) {
            throw new FileNotFoundException("File not found.", path);
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long creation = new DateTimeOffset(info.CreationTimeUtc).ToUnixTimeSeconds();
        long lastModified = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();

        FileEntry entry;
        lock (_connector) {
            entry = new FileEntry { 
                Path = path,
                Name = name, 
                Size = size, 
                Duration=duration, 
                Type = typeValue,
                Converted = converted,
                CreationDate = creation,
                LastModifiedDate = lastModified,
                RegisteredDate = now,
                Rating = rating,
                Mark = mark,
                Label = label,
                Category = category == 0 ? null : category,
                Chapters = chapters,
            };
            _entries.Add(entry);
            _connector.SaveChanges();   // insertして Idを発行させる
        }
        _changeInfoQueue.Enqueue(DataChangeInfo.Add(entry));
        return entry;
    }

    public FileEntry MediaFile2FileEntry(IMediaFile mediaFile) {
        if (mediaFile is FileEntry fileEntry) {
            return fileEntry;
        } else {
            throw new ArgumentException("mediaFile must be FileEntry");
        }
    }

    public IMediaFile Update(IMediaFile entry, string? name=null, long? size=null, long? duration=null, bool? converted=null,
        int? rating = null, int? mark = null, string? label = null, long? category = null, string? chapters = null) {
        var info = new FileInfo(entry.Path);
        if (!info.Exists) {
            throw new FileNotFoundException("File not found.", entry.Path);
        }
        long lastModified = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();

        FileEntry modified;
        lock (_connector) {
            modified = MediaFile2FileEntry(entry.Modify(name, size, duration, converted, lastModified, isDeleted: null, rating, mark, label, category, chapters));

            var tracked = _entries.Local.FirstOrDefault(it => it.Id == modified.Id);
            if (tracked != null) {
                // 追跡中のインスタンスに値をコピーする（変更された列だけが UPDATE される）
                _connector.Entry(tracked).CurrentValues.SetValues(modified);
            }
            else {
                // 追跡中でない場合は、modified を UPDATE する
                _entries.Update(modified);
            }
            // 通知用に CategoryRef を設定する（CategoryName を有効にするため）
            if (modified.Category != null) {
                modified.CategoryRef = _connector.Categories.Find(modified.Category);
            }
        }
        _changeInfoQueue.Enqueue(DataChangeInfo.Update(modified));
        return modified;
    }

    IMediaFile IMutableMediaFileStore.Update(IMediaFile entry, string? name, long? size, long? duration, bool? converted, int? rating, int? mark, string? label, long? category, string? chapters) {
        if (entry is FileEntry fileEntry) {
            return Update(fileEntry, name, size, duration, converted, rating, mark, label, category, chapters);
        }
        else {
            throw new ArgumentException("entry must be FileEntry");
        }
    }

    IMediaFile IMutableMediaFileStore.Update(IMediaFile modifiedEntry) { 
        return Update(modifiedEntry, modifiedEntry.Name, modifiedEntry.Size, modifiedEntry.Duration, modifiedEntry.Converted,
            modifiedEntry.Rating, modifiedEntry.Mark, modifiedEntry.Label, modifiedEntry.Category, modifiedEntry.Chapters);
    }

    public static string GetConvertedFilePath(string convertedSourcePath, IMediaFile entry) {
        return Path.Combine(convertedSourcePath, $"{entry.Id}");
    }

    //public void Remove(IMediaFile entry) {
    //    lock (_connector) {
    //        _entries.Remove(MediaFile2FileEntry(entry));
    //    }
    //    _changeInfoQueue.Enqueue(DataChangeInfo.Remove(entry));
    //}

    public void Remove(IEnumerable<IMediaFile> entries, string convertedSourcePath) {
        var list = entries.Select(MediaFile2FileEntry).ToList();
        if (list.Count == 0) {
            return;
        }
        lock (_connector) {
            _entries.RemoveRange(list);
        }
        foreach (var entry in list) {
            //if (entry.Converted) {    ゴミがあるかもしれないので、Convertedフラグに関係なく削除を試行する。
            // コンバートされたファイルが存在する場合は削除する
            var converted = GetConvertedFilePath(convertedSourcePath, entry);
            FileUtil.SafeDelete(converted);
            //}
            _changeInfoQueue.Enqueue(DataChangeInfo.Remove(entry));
        }
    }

    public IMediaFile? GetById(long id) {
        lock (_connector) {
            return _entries.Where(it => it.Id == id).SingleOrDefault();
        }
    }
    public IMediaFile? GetByPath(string path) {
        lock (_connector) {
            return _entries.Where(it => it.Path == path).SingleOrDefault();
        }
    }

    
}
