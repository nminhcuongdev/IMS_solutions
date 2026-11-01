namespace Ims.Api.Features.Import;

public sealed record ImportResultDto(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<ImportRowErrorDto> Errors,
    int ImportedLineCount = 0);
