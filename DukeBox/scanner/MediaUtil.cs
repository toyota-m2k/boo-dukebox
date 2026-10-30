using DukeBox.scanner.ffmpeg;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;

namespace DukeBox.scanner;

public static class MediaUtil {
    public static UtLog Logger => FileScanner.Logger;
    public static bool PreferFFMpeg { get; set; } = true;
    public static async Task<TimeSpan?> GetDurationAsync(string path) {
        if (!FileScanner.IsAVFile(path)) {
            return null;
        }
        if (PreferFFMpeg && GGConfig.UseFFMpeg) {
            try {
                var anal = await GGAnalyzer.AnalyzeAsync(path);
                return TimeSpan.FromMilliseconds(anal.DurationMs);
            }
            catch (Exception) {
                Logger.Error($"Failed to analyze media file by ffmpeg.");
            }
            // fallback ...
        }

        try { 
            var file = await StorageFile.GetFileFromPathAsync(path);
            var clip = await MediaClip.CreateFromFileAsync(file);
            return clip.OriginalDuration;
        }
        catch (Exception) {
            Logger.Error($"Failed to analyze media file by MediaClip.");
            return null;
        }
    }
}
