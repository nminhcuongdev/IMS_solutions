using Ims.Api.Contracts;

namespace Ims.Api.Features.Devices;

internal interface IDeviceService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? deviceType = null, string? updateStatus = null);

    Task<ServiceResult> GetAsync(string code, CancellationToken ct);

    Task<ServiceResult> CreateAsync(DeviceWriteDto body, CancellationToken ct);

    Task<ServiceResult> UpdateAsync(string code, DeviceWriteDto body, CancellationToken ct);

    Task<ServiceResult> DeleteAsync(string code, CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(CancellationToken ct);
}
