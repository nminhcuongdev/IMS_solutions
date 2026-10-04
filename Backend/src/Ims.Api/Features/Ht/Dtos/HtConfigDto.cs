namespace Ims.Api.Features.Ht;

public sealed record HtConfigDto(
    string LabelModeRcv,
    string LabelModeShp,
    string LabelModeRtn,
    string ReasonScope,
    string? ApkVersion,
    string? ApkDownloadUrl,
    IReadOnlyList<string> PrinterDrivers);
