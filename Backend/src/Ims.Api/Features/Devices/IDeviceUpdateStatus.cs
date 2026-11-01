namespace Ims.Api.Features.Devices;

internal interface IDeviceUpdateStatus
{
    Task<IReadOnlyCollection<string>> DeviceCodesWithStatusAsync(string status, CancellationToken ct);
}
