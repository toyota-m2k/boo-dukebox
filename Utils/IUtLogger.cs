namespace io.github.toyota32k.BooLib.Utils;

public enum LogLevel {
    VERBOSE, DEBUG, INFO, WARN, ERROR, FATAL,
}

public interface IUtLogger {
    void Output(LogLevel level, string message);
}
