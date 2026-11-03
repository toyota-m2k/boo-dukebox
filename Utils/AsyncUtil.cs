using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace io.github.toyota32k.BooLib.Utils;

public static class AsyncUtil {
    public static void Launch(Func<Task> asyncAction) {
        _ = asyncAction();
    }
}
