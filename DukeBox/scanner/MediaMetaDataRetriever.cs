using DukeBox.scanner.ffmpeg;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;
using static DukeBox.scanner.ffmpeg.GGAnalyzer;

namespace DukeBox.scanner; 

/**
 * メディアファイルから、作成日時（撮影日時）と Durationを取得するクラス
 */
public class MediaMetaDataRetriever {
    public DateTimeOffset CreationDate { get; private set; } = DateTimeOffset.MinValue;
    public TimeSpan Duration { get; private set; } = TimeSpan.Zero;
    public static MediaMetaDataRetriever Empty { get; } = new MediaMetaDataRetriever();
    private MediaMetaDataRetriever() {

    }
    public static UtLog Logger => FileScanner.Logger;
    public static bool PreferFFMpeg { get; set; } = true;
    private const string PROP_DURATION = "System.Media.Duration";   // ulong (100ns 単位)
    private const string PROP_DATE_TAKEN = "System.Photo.DateTaken";
    private const string PROP_DATE_ENCODED = "System.Media.DateEncoded";
    private const string PROP_MEDIA_YEAR = "System.Media.Year";
    private static readonly DateTimeOffset MinValid = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static bool IsMp4Family(string ext) =>
        ext.ToLowerInvariant() is ".m4a" or ".mov" or ".m4v" or ".3gp";
    private static DateTimeOffset? Validate(DateTimeOffset dto) =>
        dto < MinValid || dto > DateTimeOffset.UtcNow.AddDays(1) ? null : dto;


    /**
     * ファイルパスを与えて、MediaMetaDataRetrieverを取得する。
     */
    public static async Task<MediaMetaDataRetriever> FromPathAsync(string path) {
        var ext = Path.GetExtension(path);
        DateTimeOffset creationDate = DateTimeOffset.MinValue;
        TimeSpan duration = TimeSpan.Zero;
        try {
            if (FileScanner.IMAGE_EXTS.Contains(ext)) {
                creationDate = await GetTakenDateAsync(path);
            }
            else if (FileScanner.AV_EXTS.Contains(ext) || IsMp4Family(ext)) {
                var analysis = AnalyzeByFFProbe(path);
                creationDate = await GetEncodedDateAsync(path, analysis);
                duration = await GetDurationAsync(path, analysis) ?? TimeSpan.Zero;
            }
            return new MediaMetaDataRetriever {
                CreationDate = creationDate,
                Duration = duration
            };
        }
        catch (Exception e) {
            FileScanner.Logger.Error(e, $"failed to resolve media date: {path}");
            return MediaMetaDataRetriever.Empty;
        }
    }

    /**
     * Windows の Shell API (FileStorageのAPI) を使って Dateプロパティを取得する。
     */
    private static async Task<DateTimeOffset?> DateFromShellAsync(string path, string propName) {
        try {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var props = await file.Properties.RetrievePropertiesAsync([propName]);
            if (props.TryGetValue(propName, out var v) && v is DateTimeOffset dto) {
                return Validate(dto);
            }
        }
        catch (Exception e) {
            Logger.Error(e, $"failed to retrieve shell property: {propName}");
        }
        return null;
    }

    /**
     * FFProbe(GGAnalyzer) を使って作成日時を取得
     */
    private static async Task<DateTimeOffset?> DateFromAnalyzerAsync(string path, GGAnalyzer.Analysis? analysis) {
        if (analysis==null) {
            return null;
        }
        return analysis.CreationDate is { } dto ? Validate(dto) : null;
    }

    /**
     * Windows の Shell API (FileStorageのAPI) を使って MP3のSystem.Media.Yearプロパティを取得
     * 年しか取得できないので、価値なしｗ
     */
    //private static async Task<DateTimeOffset?> DateFromShellYearAsync(string path) {
    //    try {
    //        var file = await StorageFile.GetFileFromPathAsync(path);
    //        var props = await file.Properties.RetrievePropertiesAsync([PROP_MEDIA_YEAR]);
    //        if (props.TryGetValue(PROP_MEDIA_YEAR, out var v) && v is uint year && year >= 1980) {
    //            return new DateTimeOffset((int)year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    //        }
    //    }
    //    catch (Exception e) {
    //        Logger.Error(e, $"failed to retrieve shell property: {PROP_MEDIA_YEAR}");
    //    }
    //    return null;
    //}

    /**
     * ファイルシステムの作成日時／更新日時を取得する。
     * どちらか古い方を返す。
     * Note: 更新日時が作成日時より古いケースが（結構）ある。
     */
    private static DateTimeOffset DateFromFileSystem(string path) {
        Logger.Debug("fallback: no meta data: " + path);
        var info = new FileInfo(path);
        var c = info.CreationTimeUtc;
        var w = info.LastWriteTimeUtc;
        return new DateTimeOffset(c < w ? c : w, TimeSpan.Zero);
    }

    // PXL_20250602_021222303.jpg
    private static Regex regFormat1 = new Regex(@"[a-zA-Z_]*(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})_?(?<hour>\d{2})(?<min>\d{2})(?<sec>\d{2})");
    // for Screenshot_2025-04-25-21-23-16-593_jp.naver.line.android.jpg
    private static Regex regFormat2 = new Regex(@"[a-zA-Z_]*(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})-(?<hour>\d{2})-(?<min>\d{2})-(?<sec>\d{2})");
    private static Regex[] regFormats = [regFormat1, regFormat2];
    
    /**
     * ファイル名から作成日時を取得する。
     */
    private static DateTimeOffset? DateFromFilename(string path) {
        var filename = Path.GetFileName(path);
        foreach (var reg in regFormats) {
            var m = reg.Match(filename);
            if (m.Success) {
                var text = string.Concat(m.Groups["year"].Value, m.Groups["month"].Value, m.Groups["day"].Value,
                                         m.Groups["hour"].Value, m.Groups["min"].Value, m.Groups["sec"].Value);
                // ファイル名の日時はローカル時刻とみなす（不正な日付は TryParseExact で弾く）
                if (DateTime.TryParseExact(text, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out var dt)) {
                    return Validate(new DateTimeOffset(dt));
                }
            }
        }
        Logger.Debug("unexpected format: " + filename);
        return null;
    }

    /**
     * GGAnalyzer (ffprobe) を実行
     */
    private static GGAnalyzer.Analysis? AnalyzeByFFProbe(string path) {
        if (!PreferFFMpeg || !GGConfig.UseFFMpeg) {
            // ffmpegを使わない設定なら nullを返す。
            return null;
        }
        try {
            return GGAnalyzer.Analyze(path);
        }
        catch (Exception e) {
            FileScanner.Logger.Error(e, $"ffprobe failed: {path}");
            return null;
        }
    }

    /**
     * 画像ファイルの撮影日時を取得する。
     * Shellプロパティ --> ファイル名 --> ファイルシステムの作成日時／更新日時 の順で取得する。
     */
    private static async Task<DateTimeOffset> GetTakenDateAsync(string path) {
        return await DateFromShellAsync(path, PROP_DATE_TAKEN) 
            ?? DateFromFilename(path) 
            ?? DateFromFileSystem(path);
    }

    /**
     * 動画／音声ファイルの作成日時を取得する。
     * ffprobe --> Shellプロパティ --> ファイル名 --> ファイルシステムの作成日時／更新日時 の順で取得する。
     */
    private static async Task<DateTimeOffset> GetEncodedDateAsync(string path, Analysis? analysis) {
        return await DateFromAnalyzerAsync(path, analysis)
               ?? await DateFromShellAsync(path, PROP_DATE_ENCODED)
               ?? DateFromFilename(path)
               ?? DateFromFileSystem(path);
    }

    /**
     * 動画／音声ファイルの再生時間を取得する。
     * ffprobe --> Shell --> MediaClip の順で取得する。
     */
    private static async Task<TimeSpan?> GetDurationAsync(string path, GGAnalyzer.Analysis? analysis = null) {
        //if (!FileScanner.IsAVFile(path)) {
        //    return null;
        //}
        if (analysis != null) {
            return analysis.Duration;
        }

        StorageFile file;
        try {
            file = await StorageFile.GetFileFromPathAsync(path);
        }
        catch (Exception) {
            Logger.Error($"Failed to open media file: {path}");
            return null;
        }

        try {
            var props = await file.Properties.RetrievePropertiesAsync([PROP_DURATION]);
            if (props.TryGetValue(PROP_DURATION, out var v) && v is ulong ticks && ticks > 0) {
                return TimeSpan.FromTicks((long)ticks);
            }
        }
        catch (Exception) {
            Logger.Error($"Failed to retrieve duration property.");
        }

        try {
            var clip = await MediaClip.CreateFromFileAsync(file);
            return clip.OriginalDuration;
        }
        catch (Exception) {
            Logger.Error($"Failed to analyze media file by MediaClip.");
            return null;
        }
    }

}
