namespace Ims.Api.Features.SuperAdmin;

public sealed record VisibilityRowDto(string TargetType, string TargetCode, string FunctionName,
                                      string? LargeCode, string? LargeName, bool IsVisible);
