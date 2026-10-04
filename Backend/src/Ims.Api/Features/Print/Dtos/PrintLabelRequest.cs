using Ims.Api.Features.History;

namespace Ims.Api.Features.Print;

public sealed record PrintLabelRequest(
    string LayoutKey, string DeviceCode, Dictionary<string, string> Fields,
    PrintHistoryWriteDto History, int Copies = 1);
