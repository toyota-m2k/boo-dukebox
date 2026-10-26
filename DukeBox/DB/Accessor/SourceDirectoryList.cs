using DukeBox.model;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DukeBox.DB.Accessor; 
public class SourceDirectoryList : IMutableSourceDirectoryStore{
    public UtLog logger = new UtLog("SourceDirectoryList");
    private DBConnector _connector;
    private DbSet<SourceDirectoryEntry> _sourceDirectories;
    public SourceDirectoryList(DBConnector connector) {
        _connector = connector;
        _sourceDirectories = connector.SourceDirectories;
    }

    public IEnumerable<ISourceDirectory> List() {
        return _sourceDirectories;
    }

    public ISourceDirectory? Get(long id) {
        return _sourceDirectories.FirstOrDefault(it => it.Id == id);
    }

    public ISourceDirectory? GetByPath(string path_) {
        var path = NormalizeDirPath(path_);
        return _sourceDirectories.FirstOrDefault(it => it.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizeDirPath(string path_) {
        var path = Path.GetFullPath(path_.Trim());  // パスの正規化
        if (!path.EndsWith(Path.DirectorySeparatorChar)) {
            path += Path.DirectorySeparatorChar;
        }
        return path;
    }
    public ISourceDirectory? Add(string path_, bool recursive=true, long category=0) {
        var path = NormalizeDirPath(path_);
        if (CheckConfliction(path) != ConflictType.None) {
            logger.Debug($"Path '{path}' is already registered.");
            return null;
        }
        var entry = new SourceDirectoryEntry() {
            Path = path,
            Recursive = recursive,
            Category = category==0 ? null : category
        };
        _sourceDirectories.Add(entry);
        // _connector.SaveChanges();   // insertして Idを発行させる ... Idは使用しないので無理にsaveする必要なし。
        return entry;
    }

    public void Remove(ISourceDirectory entry) {
        _sourceDirectories.Remove((SourceDirectoryEntry)entry);
    }
    public void Remove(string path) {
        var entry = GetByPath(path);
        if (entry != null) {
            _sourceDirectories.Remove((SourceDirectoryEntry)entry);
        }
    }

    public ConflictType CheckConfliction(string path_, bool includeSubdirectories=true) {
        var path = NormalizeDirPath(path_);

        if (_sourceDirectories.Any(it => it.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) {
            return ConflictType.Registered;
        }
        // pathが登録済みのディレクトリのサブディレクトリかどうかを確認する
        foreach (var entry in _sourceDirectories.Where(it => it.Recursive)) {
            if (path.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) {
                logger.Debug($"Path '{path}' is a subdirectory of registered source directory '{entry.Path}'");
                return ConflictType.SubdirectoryOfRegistered;
            }
        }

        // includeSubdirectoriesがtrueの場合、登録済みのディレクトリがpathのサブディレクトリかどうかを確認する
        if (includeSubdirectories) {
            foreach (var entry in _sourceDirectories) {
                if (entry.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase)) {
                    logger.Debug($"Registered path '{entry.Path}' is a subdirectory of '{path}'");
                    return ConflictType.ParentDirectoryOfRegistered;
                }
            }
        }
        return ConflictType.None;
    }

}
