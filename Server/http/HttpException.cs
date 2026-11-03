using System;

namespace io.github.toyota32k.BooLib.Server.http;

public class HttpException : Exception {
    public HttpErrorResponse ErrorResponse { get; }
    public HttpException(HttpErrorResponse errorResponse):base (errorResponse.Content) { 
        ErrorResponse = errorResponse;
    }
}
