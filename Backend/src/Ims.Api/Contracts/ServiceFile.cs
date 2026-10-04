namespace Ims.Api.Contracts;

public sealed record ServiceFile(byte[] Contents, string? ContentType, string? FileName);
