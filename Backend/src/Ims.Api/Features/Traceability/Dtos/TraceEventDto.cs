namespace Ims.Api.Features.Traceability;

public sealed record TraceEventDto(string Type, decimal Qty, string? DocumentNo,
    string? PartnerCode, DateTime OccurredAt, string? SerialNo);
