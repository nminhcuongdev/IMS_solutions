namespace Ims.Api.Features.Distribution;

public sealed record AuditDto(DateTime At, string Who, string Action, string? Target);
