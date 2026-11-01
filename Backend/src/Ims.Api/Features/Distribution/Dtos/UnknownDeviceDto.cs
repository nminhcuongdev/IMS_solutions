namespace Ims.Api.Features.Distribution;

public sealed record UnknownDeviceDto(string DeviceCode, DateTime FirstAt, DateTime LastAt, int RequestCount);
