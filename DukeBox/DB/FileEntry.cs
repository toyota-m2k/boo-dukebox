using DukeBox.model;
using io.github.toyota32k.BooLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DukeBox.DB;

[Table("t_entry")]
public class FileEntry : IMediaFile {
    public static string[] DDL = {
        @"CREATE TABLE IF NOT EXISTS t_entry (
            Id INTEGER NOT NULL PRIMARY KEY,
            Path TEXT NOT NULL UNIQUE COLLATE NOCASE,                     -- ソースパス
            Name TEXT NOT NULL,
            Size INTEGER DEFAULT 0,
            Type TEXT NOT NULL,
            CopyPath TEXT,                          -- コピー先パス（コピー済みの場合のみ）
            Duration INTEGER DEFAULT 0,
            CreationDate INTEGER DEFAULT 0,         -- 元ファイルの作成日時（UnixTime）
            LastModifiedDate INTEGER DEFAULT 0,     -- 登録後、ファイルが更新された場合の最終更新日時（UnixTime）
            RegisteredDate INTEGER DEFAULT 0,       -- DBに登録した日時（UnixTime）
            Deleted INTEGER DEFAULT 0,              -- 削除フラグ（0:有効、1:削除済み）
            Rating INTEGER DEFAULT 0,
            Mark INTEGER DEFAULT 0,
            Label TEXT,
            Category INTEGER DEFAULT NULL,
            Chapters TEXT,
            CONSTRAINT FK_Category FOREIGN KEY(Category) REFERENCES t_category(Id)
        )",
    };

    [Key, Required]
    public long Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    [Required]
    public string Type { get; set; } = string.Empty;
    [Required]
    public string Path { get; set; } = string.Empty;

    public string? CopyPath { get; set; }

    public long CreationDate { get; set; }                      // ファイルの作成日時（UnixTime）
    public long RegisteredDate { get; set; }                    // SAに登録された日時（UnixTime）
    public long LastModifiedDate { get; set; }                  // ファイルの最終更新日時（UnixTime）
    public bool Deleted { get; set; }                           // 削除フラグ（0:有効、1:削除済み）
    public int Rating { get; set; }
    public int Mark { get; set; }
    public string? Label { get; set; }
    public long? Category { get; set; }
    public string? Chapters { get; set; }
    public long Duration { get; set; } = 0;

    // ナビゲーションプロパティ
    [ForeignKey(nameof(Category))]
    public CategoryEntry? CategoryRef { get; set; }

    // カテゴリー名（t_categoryテーブルのName列）を取得するプロパティ
    [NotMapped]
    public string CategoryName => CategoryRef?.Name ?? string.Empty;

    //[NotMapped]
    //public bool IsDeleted {
    //    get { return Deleted != 0; }
    //    set { Deleted = value ? 1 : 0; }
    //}

    [NotMapped]
    public string BooMediaType {
        get {
            switch(Type) {
                case "mp4":
                    return "v";
                case "png":
                case "jpg":
                case "jpeg":
                    return "p";
                case "mp3":
                case "m4a":
                    return "a";
                default:
                    return ""; // unknown
            }
        }
    }


    [NotMapped]
    public string ContentType {
        get {
            switch(Type) {
                case "mp4":
                    return "video/mp4";
                case "png":
                    return "image/png";
                case "jpg":
                case "jpeg":
                    return "image/jpeg";
                case "mp3":
                case "m4a":
                    return "audio/mpeg";
                default:
                    return "application/octet-stream"; // unknown
            }
        }
    }

    [NotMapped]
    public string FilePath => string.IsNullOrEmpty(CopyPath) ? Path : CopyPath;

    public FileEntry Modify(string? name = null, long? size = null, long? duration = null, string? copyPath = null, long? lastModified = null,
        bool? deleted = null, int? rating = null, int? mark = null, string? label = null, long? category = null, string? chapters = null)
    {
        var src = this;
        long? categoryResolved = null;
        if (category!=null) {
            categoryResolved = category == 0 ? null : category;
        }
        else {
            categoryResolved = src.Category;
        }
        return new FileEntry() { 
            Id = src.Id,
            Name = name ?? src.Name,
            Size = size ?? src.Size,
            Type = src.Type,
            Path = src.Path,
            CopyPath = copyPath ?? src.CopyPath,
            CreationDate = src.CreationDate,
            RegisteredDate = src.RegisteredDate,
            LastModifiedDate = lastModified ?? src.LastModifiedDate,
            Deleted = deleted ?? src.Deleted,
            Rating = rating ?? src.Rating,
            Mark = mark ?? src.Mark,
            Label = label ?? src.Label,
            Category = categoryResolved,
            Chapters = chapters ?? src.Chapters,
            Duration = duration ?? src.Duration
        };
    }

    //public Dictionary<string,object> ToDictionary() {
    //    return new Dictionary<string, object>() {
    //        { "id", Id },
    //        { "name", Name },
    //        { "size", Size },
    //        { "type", Type },
    //        //{ "path", Path },
    //        //{ "copyPath", CopyPath ?? "" },
    //        { "registeredDate", RegisteredDate },
    //        { "lastModifiedDate", LastModifiedDate },
    //        { "creationDate", CreationDate },
    //        { "deleted", Deleted },
    //        { "rating", Rating },
    //        { "mark", Mark },
    //        { "label", Label ?? "" },
    //        { "category", CategoryName },
    //        { "chapters", Chapters ?? "" },
    //        { "duration", Duration },
    //    };
    //}

    //public static FileEntry FromDictionary(JObject dict) {
    //    return new FileEntry() {
    //        Id = dict.GetIntValue("id"),
    //        Name = dict.GetStringValue("name", ""),
    //        Size = dict.GetLongValue("size"),
    //        Type = dict.GetStringValue("type", ""),
    //        //Path = (string)dict["path"],
    //        RegisteredDate = dict.GetLongValue("registeredDate"),
    //        LastModifiedDate = dict.GetLongValue("lastModifiedDate"),
    //        CreationDate = dict.GetLongValue("creationDate"),
    //        //MetaInfo = dict.GetStringValue("metaInfo"),
    //        Deleted = dict.GetIntValue("deleted"),

    //        //ExtAttrDate = dict.GetLongValue("extAttrDate"),
    //        Rating = dict.GetIntValue("rating"),
    //        Mark = dict.GetIntValue("mark"),
    //        Label = dict.GetStringValue("label"),
    //        Category = dict.GetStringValue("category"),
    //        Chapters = dict.GetStringValue("chapters"),
    //        Duration = dict.GetLongValue("duration"),
    //        //Slot = dict.GetIntValue("slot"),
    //    };
    //}


    private static Regex regDate = new Regex(@"(?<date>\d{4}\.\d{2}\.\d{2}-\d{2}:\d{2}:\d{2})");

    //public static string Filename2DateString(string filename) {
    //    var match = regDate.Match(filename);
    //    if (match.Success) {
    //        return match.Groups["date"].Value;
    //    }
    //    throw new ArgumentException("Filename does not contain a valid date string in the format 'yyyy.MM.dd-HH:mm:ss'.", nameof(filename));
    //}

    //private static DateTime DateStringToDateTime(string dateString) {
    //    var format = "yyyy.MM.dd-HH:mm:ss";
    //    var dt = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    //    return dt;
    //}
    //private static long DateStringToUnixTime(string dateString) {
    //    var unixTime = new DateTimeOffset(DateStringToDateTime(dateString)).ToUnixTimeMilliseconds();
    //    return unixTime;
    //}

    //public static long Filename2UnixTime(string filename) {
    //    try {
    //        var dateString = Filename2DateString(filename);
    //        return DateStringToUnixTime(dateString);
    //    }
    //    catch (Exception) {
    //        return -1;
    //    }
    //}
    //public static DateTime Filename2DateTime(string filename) {
    //    try {
    //        var dateString = Filename2DateString(filename);
    //        return DateStringToDateTime(dateString);
    //    }
    //    catch (Exception) {
    //        return DateTime.MinValue;
    //    }
    //}

    IMediaFile IMediaFile.Modify(string? name, long? size, long? duration, string? copyPath, long? lastModified, 
        bool? isDeleted, int? rating, int? mark, string? label, long? category, string? chapters) {
        int? deleted = isDeleted == null ? null : (isDeleted.Value ? 1 : 0);
        return Modify(name, size, duration, copyPath, lastModified, deleted, rating, mark, label, category, chapters);
    }
    string IMediaFile.Name => Name;
    long IMediaFile.Size => Size;
    string IMediaFile.Type => Type;
    string IMediaFile.Path => Path;
    string IMediaFile.CopyPath => CopyPath ?? string.Empty;
    long IMediaFile.CreationDate => CreationDate;
    long IMediaFile.RegisteredDate => RegisteredDate;
    long IMediaFile.LastModifiedDate => LastModifiedDate;
    long IMediaFile.Duration => Duration;

    string IMediaFile.Label => Label ?? string.Empty;
    long? IMediaFile.Category => Category;
    string IMediaFile.CategoryName => CategoryName ?? string.Empty;
    string IMediaFile.Chapters => Chapters ?? string.Empty;
}
