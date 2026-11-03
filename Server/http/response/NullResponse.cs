/**
 * URLからクエリパラメータを辞書として取り出す
 * 
 * Copyright (c) 2021 @toyota-m2k.
 */
using io.github.toyota32k.BooLib.Server.http.model;
using System.IO;

namespace io.github.toyota32k.BooLib.Server.http.response;

public class NullResponse : IHttpResponse {
    public HttpRequest Request { get; }

    public NullResponse(HttpRequest request) {
        Request = request;
    }

    public void WriteResponse(Stream output) {
        
    }
    public static NullResponse Get(HttpRequest req) { 
        return new NullResponse(req); 
    }

    public void Dispose() {
    }
}
