namespace Ims.Api.Contracts;

public static class Outcome
{
    public static ServiceResult Ok(object? value = null) => new(200, value);

    public static ServiceResult Created(string? uri, object? value) => new(201, value, uri);

    public static ServiceResult NoContent() => new(204);

    public static ServiceResult BadRequest(object? error = null) => new(400, error);

    public static ServiceResult NotFound(object? value = null) => new(404, value);

    public static ServiceResult Conflict(object? error = null) => new(409, error);

    public static ServiceResult Json(object? data, int? statusCode = null) => new(statusCode ?? 200, data);

    public static ServiceResult File(byte[] fileContents, string? contentType = null, string? fileDownloadName = null) =>
        new(200, file: new ServiceFile(fileContents, contentType, fileDownloadName));

    public static ServiceResult Stream(Func<Stream, Task> streamWriterCallback, string? contentType = null, string? fileDownloadName = null) =>
        new(200, stream: new ServiceStream(streamWriterCallback, contentType, fileDownloadName));
}
