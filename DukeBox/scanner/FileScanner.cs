using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DukeBox.scanner;

public class FileScanner {
    public static UtLog Logger = new UtLog("Scanner");

    public static readonly HashSet<string> VIDEO_EXTS = new([".mp4",], StringComparer.OrdinalIgnoreCase);
    public static readonly HashSet<string> AUDIO_EXTS = new([".mp3",], StringComparer.OrdinalIgnoreCase);
    public static readonly HashSet<string> IMAGE_EXTS = new([".jpg", ".png", ".jpeg"], StringComparer.OrdinalIgnoreCase);
    public static readonly HashSet<string> MEDIA_EXTS = new([.. VIDEO_EXTS, .. AUDIO_EXTS, .. IMAGE_EXTS], StringComparer.OrdinalIgnoreCase);
    public static readonly HashSet<string> AV_EXTS = new([.. VIDEO_EXTS, .. AUDIO_EXTS], StringComparer.OrdinalIgnoreCase);

    public static bool IsMediaFile(string filePath) {
        var ext = Path.GetExtension(filePath);
        return MEDIA_EXTS.Contains(ext);
    }
    public static bool IsAVFile(string filePath) {
        var ext = Path.GetExtension(filePath);
        return AV_EXTS.Contains(ext);
    }

    /**
     * @param path: directory path to scan
     * @param recursive: whether to scan subdirectories recursively
     * @param filter: optional filter function to apply to each file
     */
    public static IEnumerable<FileInfo> ListFilesInDirectory(string path, bool recursive = true, Func<FileInfo, bool>? filter = null) {
        var options = new EnumerationOptions {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,          // アクセス拒否を無視（読めないディレクトリがあっても例外を投げない）
            MatchCasing = MatchCasing.CaseInsensitive,
        };
        var dir = new DirectoryInfo(path);
        if (filter == null) {
            return dir.EnumerateFiles("*", options);
        }
        else {
            return dir.EnumerateFiles("*", options).Where(filter);
        }
    }

    /**
     * @param path: directory path to scan
     * @param exts: optional set of file extensions to filter by
     * @param recursive: whether to scan subdirectories recursively
     */
    public static IEnumerable<FileInfo> ListFilesInDirectory(string path, HashSet<string>? exts, bool recursive = true) {
        if (exts == null) {
            return ListFilesInDirectory(path, recursive, null);
        }
        else {
            return ListFilesInDirectory(path, recursive, f => exts.Contains(f.Extension));
        }
    }

    public HashSet<string>? Exts { get; }
    public bool Recursive { get; }
    public string DirectoryPath { get; }
    public long Category { get; }
    private List<FileInfo> _files = new();

    public FileScanner(string directoryPath, long category, HashSet<string>? exts, bool recursive = true) {
        DirectoryPath = directoryPath;
        Exts = exts;
        Recursive = recursive;
        Category = category;
    }

    public FileScanner(string directoryPath, long category, bool recursive = true) {
        DirectoryPath = directoryPath;
        Exts = MEDIA_EXTS;
        Recursive = recursive;
        Category = category;
    }

    /**
     * 現在の（最後にScan()した時点の）ディレクトリ内のファイルリストを返す。
     */
    public IEnumerable<FileInfo> Files => _files.AsReadOnly();

    /**
     * 初期化処理。
     * 前回のアプリ終了時の状態を復元し、且つ、アプリ終了後に削除されたファイルのリストを返す。
     */
    public List<string> RestoreWith(IEnumerable<string> initialFiles) {
        var files = initialFiles.ToList();
        _files = files.Select(f => new FileInfo(f)).Where(it=>it.Exists).ToList();
        var hashSet = new HashSet<string>(_files.Select(f => f.FullName), StringComparer.OrdinalIgnoreCase);
        var removedFiles = files.Where(f => !hashSet.Contains(f)).ToList();
        return removedFiles;
    }

    /**
     * ディレクトリ毎に保持しているファイルリスト(_files)を更新し、
     * 前回からの差分（追加ファイル、削除ファイル）を返す。
     */
    public (List<FileInfo> addedFiles, List<string> removedFiles) Scan() {
        var addedFiles = new List<FileInfo>();
        var removedFiles = new List<string>();
        var hsetPrevFiles = new HashSet<string>(_files.Select(f => f.FullName), StringComparer.OrdinalIgnoreCase);

        var currentFiles = ListFilesInDirectory(DirectoryPath, Exts, Recursive).ToList();
        var hsetCurrentFiles = new HashSet<string>(currentFiles.Select(f => f.FullName), StringComparer.OrdinalIgnoreCase);

        foreach (var file in currentFiles) {
            if (!hsetPrevFiles.Contains(file.FullName)) {
                _files.Add(file);
                addedFiles.Add(file);
            }
        }

        foreach (var file in hsetPrevFiles.ToList()) {
            if (!hsetCurrentFiles.Contains(file)) {
                _files.Remove(_files.First(f => f.FullName == file));
                removedFiles.Add(file);
            }
        }

        return (addedFiles, removedFiles);
    }

}