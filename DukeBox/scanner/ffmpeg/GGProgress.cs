using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.scanner.ffmpeg;

public enum GGProcessId {
    NONE,
    ANALYZING,
    TRIMMING,
    SPLITTING,
    COMBINING,
    COMPRESSING,
    DISPOSING,
}

public interface IGGProgress {
    GGProcessId ProcessId { get; set; } // 現在の処理内容
    double Percent { get; set; } // 進捗率
    string ProgressMessage { get; set; } // 進捗メッセージ (例: "1/10 (10%)")
}
