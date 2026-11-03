using io.github.toyota32k.BooLib.Server.http.model;
using System.IO;

namespace io.github.toyota32k.BooLib.Server.http.response;

public class FileHttpResponse : AbstractHttpResponse {
    public string ContentFilePath { get; set; }
    protected long FileLength => new FileInfo(ContentFilePath).Length;

    public FileHttpResponse(HttpRequest req, HttpStatusCode statusCode, string contentType, string filePath) : base(req, statusCode) {
        ContentFilePath = filePath;
        ContentType = contentType;
    }

    protected FileStream OpenFile() {
        return new FileStream(ContentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    protected override void Prepare() {
        ContentLength = FileLength;
    }

    protected override void WriteBody(Stream output) {
        using (var input = OpenFile()) {
            input.CopyTo(output);
            output.Flush();
        }
    }
    // informational only tostring...
    public override string ToString() {
        return string.Format($"FileHttpResponse status {(int)StatusCode} {StatusCode}");
    }
}

