namespace Ims.Api.Features.Devices;

public sealed record DeviceWriteDto(
    string DeviceCode,
    string DeviceName,
    string DeviceType,
    string? Model = null,
    string? Manufacturer = null,
    string? Spec = null,
    string? SerialNo = null,
    string? IpAddress = null,
    string? LocationCode = null,
    bool IsActive = true,
    string? Remark1 = null,
    string? Remark2 = null,
    string? Remark3 = null,
    string? Remark4 = null,
    string? Remark5 = null);
