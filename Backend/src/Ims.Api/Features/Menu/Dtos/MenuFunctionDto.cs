namespace Ims.Api.Features.Menu;

public sealed record MenuFunctionDto(
    string FunctionCode,
    string FunctionName,
    int? DisplayOrder);
