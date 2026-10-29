using DukeBox.DB;
using DukeBox.model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DukeBox.DI.contract;

/**
 * IDBService is the interface for the database service.
 * It provides access to the media file store, key-value store, and category store.
 */
public interface ITransaction: IDisposable {
    bool AutoCommitOnDisposed { get; set; }
    void Commit();
    void Rollback();
}

public interface IDBService {
    // Media Store
    IMediaFileStore MediaFileStore { get; }
    bool EditMediaFileStore(Func<IMutableMediaFileStore, bool> fn);
    Task<bool> EditMediaFileStore(Func<IMutableMediaFileStore, Task<bool>> fn);
    
    // KV Store
    IKVStore KVStore { get; }
    bool EditKVStore(Func<IMutableKVStore, bool> fn);

    // Category Store
    ICategoryStore CategoryStore { get; }
    bool EditCategoryStore(Func<IMutableCategoryStore, bool> fn);
    Task<bool> EditCategoryStore(Func<IMutableCategoryStore, Task<bool>> fn);

    // Source Directory Store
    ISourceDirectoryStore SourceDirectoryStore { get; }
    bool EditSourceDirectoryStore(Func<IMutableSourceDirectoryStore, bool> fn);
    Task<bool> EditSourceDirectoryStore(Func<IMutableSourceDirectoryStore, Task<bool>> fn);

    ITransaction BeginTransaction(bool saveOnDispose=true);

    // このプロパティは、SettingsServiceを参照したいところだが、SettingsServiceは、そのストレージとして、
    // DBService.KVStoreを利用しているため、DBServiceから、SettingsServiceを参照すると、循環参照になってしまう。
    // そのため、SettingsServiceのConvertedSourcePathプロパティの値を、DBServiceにコピーして利用する。
    string ConvertedSourcePath { get; set; }
    string GetConvertedFilePath(IMediaFile entry);
    void RemoveSourceDirectory(ISourceDirectory srcDirEntry);
    void RemoveCategory(CategoryEntry category);
    void RemoveMediaFiles(IEnumerable<IMediaFile> files);
}

