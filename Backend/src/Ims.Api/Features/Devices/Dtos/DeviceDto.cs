namespace Ims.Api.Features.Devices;

public sealed record DeviceDto(
    string DeviceCode,
    string DeviceName,
    string DeviceType,
    string? Model,
    string? Manufacturer,
    string? Spec,
    string? SerialNo,
    string? IpAddress,
    string? LocationCode,
    bool IsActive,
    string? Remark1,
    string? Remark2,
    string? Remark3,
    string? Remark4,
    string? Remark5,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
