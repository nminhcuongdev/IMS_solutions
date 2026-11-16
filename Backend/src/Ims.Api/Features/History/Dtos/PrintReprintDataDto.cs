namespace Ims.Api.Features.History;

public sealed record PrintReprintDataDto(string LayoutFile, Dictionary<string, string>? Fields, PrintSourceDto? Source);
