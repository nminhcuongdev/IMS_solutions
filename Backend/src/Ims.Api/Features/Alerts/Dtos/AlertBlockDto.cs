namespace Ims.Api.Features.Alerts;

public sealed record AlertBlockDto(
    string AlertTypeCode,
    string LabelKey,
    string ExtraLabelKey,
    int Total,
    IReadOnlyList<AlertRowDto> Rows);
