namespace Ims.Api.Contracts;

public sealed record ServiceStream(Func<Stream, Task> Write, string? ContentType, string? FileName);
