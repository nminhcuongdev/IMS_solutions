namespace Ims.Api.Features.History;

public sealed record RfidGateHistoryDto(
    long No, DateTime WorkedDate, string? ItemCode, string? ItemName,
    string GateCode, string? SerialNo, string? LotNo, string? DirectionType, int Status);
