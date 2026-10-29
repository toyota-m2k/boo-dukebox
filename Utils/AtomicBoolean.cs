using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace io.github.toyota32k.BooLib.Utils;

public class AtomicBoolean {
    private int _value;

    public AtomicBoolean(bool initialValue = false) {
        _value = initialValue ? 1 : 0;
    }

    // 現在の値を取得・設定 (Exchangeによりアトミックに設定)
    public bool Value {
        get => _value == 1;
        set => Interlocked.Exchange(ref _value, value ? 1 : 0);
    }

    // 値が expected と等しい場合のみ update に更新し、成功したかどうかを返す
    public bool CompareAndSet(bool expected, bool update) {
        int expectedInt = expected ? 1 : 0;
        int updateInt = update ? 1 : 0;

        // 戻り値は「更新前の値」
        return Interlocked.CompareExchange(ref _value, updateInt, expectedInt) == expectedInt;
    }
}