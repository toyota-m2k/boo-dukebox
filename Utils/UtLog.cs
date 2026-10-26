using System;
using System.Runtime.CompilerServices;

namespace io.github.toyota32k.BooLib.Utils;

public class UtLog {
    public static UtLoggerChain GlobalLoggerChain { get; } = new UtLoggerChain();

    /**
     * new UtLog("prefix").Debug("message"); と書くのがなんか気持ち悪い（個人の感想です）ので、
     * UtLog.Create("prefix").Debug("message"); と書けるようにした。
     */
    public static UtLog Create(string prefix) {
        return new UtLog(prefix);
    }
    public static UtLog Create(Type type) {
        return new UtLog(type);
    }

    private string _prefix;
    UtLoggerChain? LoggerChain { get; set; }
    
    private IUtLogger MyLogger => LoggerChain ?? GlobalLoggerChain;

    public UtLog(string prefix, UtLog? parent = null, UtLoggerChain? loggerChain = null) {
        _prefix = parent!=null ? parent._prefix + "." + prefix : prefix;
        LoggerChain = loggerChain;
    }
    public UtLog(Type clazz, UtLog? parent = null, UtLoggerChain? loggerChain = null) {
        _prefix = parent != null ? parent._prefix + "." + clazz.Name : clazz.Name;
        LoggerChain = loggerChain;
    }

    string composeMessage(string msg, string memberName, int sourceLineNumber) {
        //var time = $"{DateTime.Now.ToLocalTime()}";
        return $"{DateTime.Now.ToLocalTime()} {_prefix}.{memberName}({sourceLineNumber}) {msg}";
    }

    public void Verbose(string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        var m = composeMessage(message, memberName, sourceLineNumber);
        MyLogger.Output(LogLevel.VERBOSE, m);
    }

    public void Debug(string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        var m = composeMessage(message, memberName, sourceLineNumber);
        MyLogger.Output(LogLevel.DEBUG, m);
    }
    public void Info(string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        MyLogger.Output(LogLevel.INFO, composeMessage(message, memberName, sourceLineNumber));
    }
    public void Warn(string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        MyLogger.Output(LogLevel.WARN, composeMessage(message, memberName, sourceLineNumber));
    }
    public void Error(string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        MyLogger.Output(LogLevel.ERROR, composeMessage(message, memberName, sourceLineNumber));
    }
    public void Error(Exception exception, string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        MyLogger.Output(LogLevel.ERROR, composeMessage(message, memberName, sourceLineNumber));
    }
    public void Fatal(string message = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        MyLogger.Output(LogLevel.FATAL, composeMessage(message, memberName, sourceLineNumber));
    }

    public void Log(LogLevel level, string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) {
        MyLogger.Output(level, composeMessage(message, memberName, sourceLineNumber));
    }

    #region Chronos

    void Chronos(LogLevel level, string message, Action action) {
        var time = DateTime.Now;
        try {
            action();
        }
        finally {
            var elapsed = DateTime.Now - time;
            Log(level, $"{message} {elapsed}");
        }
    }
    T Chronos<T>(LogLevel level, string message, Func<T> func) {
        var time = DateTime.Now;
        try {
            return func();
        }
        finally {
            var elapsed = DateTime.Now - time;
            Log(level, $"{message} {elapsed}");
        }
    }

    void Chronos(string message, Action action) => Chronos(LogLevel.DEBUG, message, action);
    T Chronos<T>(string message, Func<T> func) => Chronos(LogLevel.DEBUG, message, func);

    #endregion
}

public class Chronos(UtLog logger, LogLevel level = LogLevel.DEBUG) : IDisposable {
    DateTime Time = DateTime.Now;
    LogLevel Level = level;
    public void Start(string? message = null) {
        if (message != null) logger.Log(Level, message);
        Time = DateTime.Now;
    }

    public void Lap(string message) {
        var elapsed = DateTime.Now - Time;
        logger.Log(Level, $"{message} {elapsed.Seconds}.{elapsed.Milliseconds} sec");
    }

    public void Stop(string message) {
        var elapsed = DateTime.Now - Time;
        logger.Log(Level, $"{message} {elapsed.Seconds}.{elapsed.Milliseconds} sec");
    }

    public void Dispose() {
        Stop("Total");
    }
}
