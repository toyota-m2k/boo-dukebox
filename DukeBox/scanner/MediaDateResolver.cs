using System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace DukeBox.scanner;

/// <summary>
/// メディアファイルの「撮影日時／録画・録音日時」を推定する。
/// 優先: シェルプロパティ(WinRT) → ffprobe → ファイルシステム日時
/// </summary>
public static class MediaDateResolver {
    private const string PROP_DATE_TAKEN = "System.Photo.DateTaken";
    private const string PROP_DATE_ENCODED = "System.Media.DateEncoded";
    private const string PROP_MEDIA_YEAR = "System.Media.Year";

    /// <summary>ffprobe.exe のパス。null なら ffprobe フォールバックを使わない。</summary>
    public static string? FFProbePath { get; set; }

    private static readonly DateTimeOffset MinValid = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static async Task<DateTimeOffset> ResolveAsync(string path) {
        var ext = Path.GetExtension(path);
        DateTimeOffset? result = null;
        try {
            if (FileScanner.IMAGE_EXTS.Contains(ext)) {
                result = await FromShellAsync(path, PROP_DATE_TAKEN);
            }
            else if (FileScanner.AV_EXTS.Contains(ext) || IsMp4Family(ext)) {
                // iPhone の com.apple.quicktime.creationdate はタイムゾーン付きで最も正確なので ffprobe を優先
                result = await FromFFProbeAsync(path) ?? await FromShellAsync(path, PROP_DATE_ENCODED);
                if (result == null && ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase)) {
                    result = await FromShellYearAsync(path);
                }
            }
        }
        catch (Exception e) {
            FileScanner.Logger.Error(e, $"failed to resolve media date: {path}");
        }
        return result ?? FromFileSystem(path);
    }

    private static bool IsMp4Family(string ext) =>
        ext is ".m4a" or ".mov" or ".m4v" or ".3gp";

    private static async Task<DateTimeOffset?> FromShellAsync(string path, string propName) {
        var file = await StorageFile.GetFileFromPathAsync(path);
        var props = await file.Properties.RetrievePropertiesAsync([propName]);
        if (props.TryGetValue(propName, out var v) && v is DateTimeOffset dto) {
            return Validate(dto);
        }
        return null;
    }

    private static async Task<DateTimeOffset?> FromShellYearAsync(string path) {
        var file = await StorageFile.GetFileFromPathAsync(path);
        var props = await file.Properties.RetrievePropertiesAsync([PROP_MEDIA_YEAR]);
        if (props.TryGetValue(PROP_MEDIA_YEAR, out var v) && v is uint year && year >= 1980) {
            return new DateTimeOffset((int)year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }
        return null;
    }

    private static async Task<DateTimeOffset?> FromFFProbeAsync(string path) {
        if (string.IsNullOrEmpty(FFProbePath) || !File.Exists(FFProbePath)) return null;

        var psi = new ProcessStartInfo(FFProbePath) {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", path }) {
            psi.ArgumentList.Add(a);
        }
        using var proc = Process.Start(psi);
        if (proc == null) return null;
        var json = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        using var doc = JsonDocument.Parse(json);
        var candidates = new List<string>();
        if (doc.RootElement.TryGetProperty("format", out var format) &&
            format.TryGetProperty("tags", out var tags)) {
            CollectTags(tags, candidates);
        }
        if (doc.RootElement.TryGetProperty("streams", out var streams)) {
            foreach (var s in streams.EnumerateArray()) {
                if (s.TryGetProperty("tags", out var st)) CollectTags(st, candidates);
            }
        }
        foreach (var c in candidates) {
            if (DateTimeOffset.TryParse(c, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var dto) && Validate(dto) is { } valid) {
                return valid;
            }
        }
        return null;
    }

    private static void CollectTags(JsonElement tags, List<string> list) {
        // 優先順位順（キーは大文字小文字が揺れるので比較で拾う）
        string[] keys = ["com.apple.quicktime.creationdate", "creation_time", "date"];
        foreach (var key in keys) {
            foreach (var p in tags.EnumerateObject()) {
                if (p.Name.Equals(key, StringComparison.OrdinalIgnoreCase) &&
                    p.Value.GetString() is { Length: > 0 } s) {
                    list.Add(s);
                }
            }
        }
    }

    private static DateTimeOffset? Validate(DateTimeOffset dto) =>
        dto < MinValid || dto > DateTimeOffset.UtcNow.AddDays(1) ? null : dto;

    private static DateTimeOffset FromFileSystem(string path) {
        var info = new FileInfo(path);
        var c = info.CreationTimeUtc;
        var w = info.LastWriteTimeUtc;
        return new DateTimeOffset(c < w ? c : w, TimeSpan.Zero);
    }
}