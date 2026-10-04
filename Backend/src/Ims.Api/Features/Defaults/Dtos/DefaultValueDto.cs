namespace Ims.Api.Features.Defaults;

public sealed record DefaultValueDto(
    string ValueType,
    string ValueCode,
    string NameLang1,
    string NameLang2,
    string NameLang3,
    int DisplayOrder,
    bool IsActive);
