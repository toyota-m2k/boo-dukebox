using System.Collections.Generic;

namespace io.github.toyota32k.BooLib.Utils;

public class UtLoggerChain : IUtLogger {
    private readonly List<IUtLogger> loggers = new List<IUtLogger>() { new UtDebugLogger() };

    public static UtLoggerChain operator +(UtLoggerChain chain, IUtLogger logger) {
        return chain.AddLogger(logger);
    }
    public static UtLoggerChain operator -(UtLoggerChain chain, IUtLogger logger) {
        return chain.RemoveLogger(logger);
    }

    public UtLoggerChain AddLogger(IUtLogger logger) {
        loggers.Add(logger);
        return this;
    }

    public UtLoggerChain RemoveLogger(IUtLogger logger) {
        loggers.Remove(logger);
        return this;
    }

    public UtLoggerChain ClearLoggers() {
        loggers.Clear();
        return this;
    }

    public UtLoggerChain ReplaceLoggers(params IUtLogger[] newLoggers) {
        loggers.Clear();
        loggers.AddRange(newLoggers);
        return this;
    }

    public void Output(LogLevel level, string message) {
        foreach (var logger in loggers) {
            logger.Output(level, message);
        }
    }
}
