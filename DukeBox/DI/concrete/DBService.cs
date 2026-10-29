using DukeBox.DB;
using DukeBox.DB.Accessor;
using DukeBox.DI.contract;
using DukeBox.model;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DukeBox.DI.concrete; 
public class DBService : IDBService {
    private readonly IAppConfigService ConfigService;
    private readonly IMainThreadService MainThreadService;
    private DBConnector Connector;
    private IMutableMediaFileStore MutableMediaFileStore;
    public IMutableKVStore KVStore { get; }
    private IMutableCategoryStore MutableCategoryStore;
    private readonly SemaphoreSlim _dbGate = new(1, 1);

    public IMediaFileStore MediaFileStore => MutableMediaFileStore;
    public ICategoryStore CategoryStore => MutableCategoryStore;
    public IMutableSourceDirectoryStore MutableSourceDirectoryStore;
    public ISourceDirectoryStore SourceDirectoryStore => MutableSourceDirectoryStore;
    public string ConvertedSourcePath { get; set; } = string.Empty;
    private string ActConvertedSourcePath => string.IsNullOrEmpty(ConvertedSourcePath) ? Path.Combine(ConfigService.AppDataPath, "Conv") : ConvertedSourcePath;

    public DBService(IAppConfigService configService, IMainThreadService mainThreadService) {
        this.ConfigService = configService;
        this.MainThreadService = mainThreadService;
        Connector = new DBConnector(ConfigService.DBPath);
        MutableMediaFileStore = new FileEntryList(Connector);
        KVStore = new KVList(Connector);
        MutableCategoryStore = new CategoryList(Connector);
        MutableSourceDirectoryStore = new SourceDirectoryList(Connector);
    }

    public bool EditMediaFileStore(Func<IMutableMediaFileStore, bool> fn) {
        bool result = false;
        _dbGate.Wait();
        try {
            try {
                result = fn(MutableMediaFileStore);
                return result;
            }
            finally {
                if (result) {
                    Connector.SaveChanges();
                }
            }
        }
        finally {
            _dbGate.Release();
            if (result) {
                MainThreadService.Run(MutableMediaFileStore.ChangeEventSource.Submit);
            }
        }
    }

    public async Task<bool> EditMediaFileStore(Func<IMutableMediaFileStore, Task<bool>> fn) {
        bool result = false;
        await _dbGate.WaitAsync();
        try {
            try {
                result = await fn(MutableMediaFileStore);
                return result;
            }
            finally {
                if (result) {
                    Connector.SaveChanges();
                }
            }
        }
        finally {
            _dbGate.Release();
            if (result) {
                MainThreadService.Run(MutableMediaFileStore.ChangeEventSource.Submit);
            }
        }
    }

    //public bool EditKVStore(Func<IMutableKVStore, bool> fn) {
    //    bool result = false;
    //    _dbGate.Wait();
    //    try {
    //        try {
    //            result = fn(MutableKVStore);
    //            return result;
    //        }
    //        finally {
    //            if (result) {
    //                Connector.SaveChanges();
    //            }
    //        }
    //    }
    //    finally {
    //        _dbGate.Release();
    //    }
    //}


    public bool EditCategoryStore(Func<IMutableCategoryStore, bool> fn) {
        bool result = false;
        _dbGate.Wait();
        try {
            try {
                result = fn(MutableCategoryStore);
                return result;
            }
            finally {
                if (result) {
                    Connector.SaveChanges();
                }
            }
        }
        finally {
            _dbGate.Release();
        }
    }

    public async Task<bool> EditCategoryStore(Func<IMutableCategoryStore, Task<bool>> fn) {
        bool result = false;
        await _dbGate.WaitAsync();
        try {
            try {
                result = await fn(MutableCategoryStore);
                return result;
            }
            finally {
                if (result) {
                    Connector.SaveChanges();
                }
            }
        }
        finally {
            _dbGate.Release();
        }
    }

    public bool EditSourceDirectoryStore(Func<IMutableSourceDirectoryStore, bool> fn) {
        bool result = false;
        _dbGate.Wait();
        try {
            try {
                result = fn(MutableSourceDirectoryStore);
                return result;
            }
            finally {
                if (result) {
                    Connector.SaveChanges();
                }
            }
        }
        finally {
            _dbGate.Release();
        }

    }

    public async Task<bool> EditSourceDirectoryStore(Func<IMutableSourceDirectoryStore, Task<bool>> fn) {
        bool result = false;
        await _dbGate.WaitAsync();
        try {
            try {
                result = await fn(MutableSourceDirectoryStore);
                return result;
            }
            finally {
                if (result) {
                    Connector.SaveChanges();
                }
            }
        }
        finally {
            _dbGate.Release();
        }
    }

    private void removeSourceDirectoriesCore(IMutableMediaFileStore store, IEnumerable<ISourceDirectory> srcDirEntries) {
        foreach (var srcDirEntry in srcDirEntries) {
            IList<IMediaFile> files;
            if (srcDirEntry.Recursive) {
                files = store.Enumerable().Where(it => {
                    return it.Path.StartsWith(srcDirEntry.Path, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }
            else {
                files = store.Enumerable().Where(it => {
                    var fileName = Path.GetFileName(it.Path);
                    return it.Path.Equals(Path.Combine(srcDirEntry.Path, fileName), StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }
            ((FileEntryList)store).Remove(files, ActConvertedSourcePath);
            ((SourceDirectoryList)MutableSourceDirectoryStore).Remove(srcDirEntry);
        }

    }

    /**
     * SourceDirectoryStoreから、ソースディレクトリを削除する。
     * 削除するソースディレクトリに含まれるメディアファイルも削除される。
     * 
     * @param srcDirEntry 削除するソースディレクトリのエントリ
     */
    //public void RemoveSourceDirectory(IEnumerable<ISourceDirectory> srcDirEntries) {
    //    using (var txn = BeginTransaction(true)) {
    //        removeSourceDirectoriesCore(srcDirEntries);
    //    }
    //}
    public void RemoveSourceDirectory(ISourceDirectory srcDirEntry) {
        using (var txn = BeginTransaction(true)) {
            EditMediaFileStore(store => {
                removeSourceDirectoriesCore(store, new[] { srcDirEntry });
                return true;
            });
        }
    }

    /**
     * CategoryStoreから、カテゴリを削除する。
     * 削除するカテゴリに含まれるSourceDirectoryと、その中のメディアファイルも削除される。
     * 
     * @param category 削除するカテゴリのエントリ
     */
    public void RemoveCategory(CategoryEntry category) {
        using (var txn = BeginTransaction(true)) {
            EditMediaFileStore(store => {
                var files = store.Enumerable().Where(it => it.Category == category.Id).ToList();
                ((FileEntryList)store).Remove(files, ActConvertedSourcePath);
                var dirsInCategory = SourceDirectoryStore.List().Where(it => it.Category == category.Id).ToList();
                removeSourceDirectoriesCore(store, dirsInCategory);
                ((CategoryList)MutableCategoryStore).Remove(category);
                return true;
            });
        }
    }

    public void RemoveMediaFiles(IEnumerable<IMediaFile> files) {
        using (var txn = BeginTransaction(true)) {
            EditMediaFileStore(store => {
                ((FileEntryList)store).Remove(files, ActConvertedSourcePath);
                return true;
            });
        }
    }

    public string GetConvertedFilePath(IMediaFile entry) {
        return FileEntryList.GetConvertedFilePath(ActConvertedSourcePath, entry);
    }

    private class Transaction : ITransaction {
        private readonly DBConnector _dbConnector;
        private IDbContextTransaction? _txn = null;
        public bool AutoCommitOnDisposed { get; set; } = true;

        public Transaction(DBConnector dbConnector, bool autoCommitOnDisposed = true) {
            _dbConnector = dbConnector;
            AutoCommitOnDisposed = autoCommitOnDisposed;
            _txn = _dbConnector.Database.BeginTransaction();
        }

        public void Commit() {
            _txn?.Commit();
            _txn = null;
        }

        public void Rollback() {
            _txn?.Rollback();
            _txn = null;
        }

        public void Dispose() {
            if (AutoCommitOnDisposed) {
                Commit();
            } else {
                Rollback();
            }
        }
    }

    public ITransaction BeginTransaction(bool saveOnDispose=true) {
        return new Transaction(Connector, saveOnDispose);
    }

}
