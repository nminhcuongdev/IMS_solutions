namespace Ims.Api.Contracts;

public sealed class ServiceResult
{
    internal ServiceResult(int statusCode, object? body = null, string? location = null,
        ServiceFile? file = null, ServiceStream? stream = null)
    {
        StatusCode = statusCode;
        Body = body;
        Location = location;
        File = file;
        Stream = stream;
    }

    public int StatusCode { get; }

    public object? Body { get; }

    public string? Location { get; }

    public ServiceFile? File { get; }

    public ServiceStream? Stream { get; }
}
