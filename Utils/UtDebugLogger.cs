namespace io.github.toyota32k.BooLib.Utils;

public class UtDebugLogger : IUtLogger {
    public static LogLevel MinLevel { get; set; } = LogLevel.DEBUG;
    public void Output(LogLevel level, string message) {
        if (level < MinLevel) {
            return;
        }
        System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
    }
}
