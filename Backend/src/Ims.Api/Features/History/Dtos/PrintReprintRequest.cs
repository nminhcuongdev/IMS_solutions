namespace Ims.Api.Features.History;

public sealed record PrintReprintRequest(string DeviceCode, int Count, Dictionary<string, string> Fields);
