using io.github.toyota32k.BooLib.Server.http.model;
using io.github.toyota32k.BooLib.Server.http.response;
using System;
using System.IO;

namespace io.github.toyota32k.BooLib.Server.http;

public class HttpCorsException : Exception {
    public HttpCorsOptionsResponse Response { get; }
    public HttpCorsException(HttpRequest req) : base("CorsException") {
        Response = new HttpCorsOptionsResponse(req);
    }
}
public class HttpCorsOptionsResponse : AbstractHttpResponse {
    public HttpCorsOptionsResponse(HttpRequest req) : base(req, HttpStatusCode.NoContent) {
    }

    protected override void Prepare() {
        
    }

    protected override void WriteBody(Stream output) {
        
    }

    public override void WriteResponse(Stream output) {
        WriteHeaders(output);
        output.Flush();
    }

}
