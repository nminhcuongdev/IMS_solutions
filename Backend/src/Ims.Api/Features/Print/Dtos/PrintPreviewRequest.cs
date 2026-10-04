namespace Ims.Api.Features.Print;

public sealed record PrintPreviewRequest(string LayoutKey, string DeviceCode, Dictionary<string, string> Fields);
