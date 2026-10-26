using io.github.toyota32k.BooLib.Server.http.model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace io.github.toyota32k.BooLib.Server.http.response;

public class TextHttpResponse : AbstractHttpResponse {
    public const string CT_TEXT_PLAIN = "text/plain";
    public const string CT_TEXT_HTML = "text/html";
    public const string CT_JSON = "application/json";

    public string Content { get; set; }
    private byte[] Buffer = null!;

    public TextHttpResponse(HttpRequest req, HttpStatusCode statusCode, string content, string contentType = CT_TEXT_PLAIN) : base(req, statusCode) {
        Content = content;
        ContentType = contentType;
    }

    public static TextHttpResponse FromJson(HttpRequest req, IDictionary<string,object> jsonDic, HttpStatusCode statusCode=HttpStatusCode.Ok) {
        return new TextHttpResponse(req, statusCode, JsonConvert.SerializeObject(jsonDic), CT_JSON);
    }
    public static TextHttpResponse FromJsonString(HttpRequest req, string json, HttpStatusCode statusCode = HttpStatusCode.Ok) {
        return new TextHttpResponse(req, statusCode, json, CT_JSON);
    }

    protected override void Prepare() {
        Buffer = Content != null ? Encoding.UTF8.GetBytes(Content) : new byte[] { };
        ContentLength = Buffer.Length;
    }

    protected override void WriteBody(Stream output) {
        output.Write(Buffer, 0, Buffer.Length);
    }

    // informational only tostring...
    public override string ToString() {
        return string.Format($"TextHttpResponse status {(int)StatusCode} {StatusCode}");
    }
}
