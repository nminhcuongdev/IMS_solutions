using System.Text;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Contracts;
using Ims.Domain.Masters;
using Ims.Infrastructure.Persistence;
using static Ims.Api.Contracts.FieldHelpers;

namespace Ims.Api.Features.Devices;

internal sealed class DeviceService(ImsDbContext db, IDeviceUpdateStatus? statusSource = null) : IDeviceService
{
    private SortMap<Device> Sort => _sort ??= new SortMap<Device>()
        .Add("app_version", d => db.DistDeviceStates.Where(s => s.DeviceCode == d.DeviceCode).Select(s => s.VersionCode).FirstOrDefault())
        .Add("last_update_at", d => db.DistDeviceStates.Where(s => s.DeviceCode == d.DeviceCode).Select(s => s.LastUpdateAt).FirstOrDefault())
        .Add("last_check_at", d => db.DistDeviceStates.Where(s => s.DeviceCode == d.DeviceCode).Select(s => s.LastCheckAt).FirstOrDefault())
        .Add("device_code", d => d.DeviceCode)
        .Add("device_name", d => d.DeviceName)
        .Add("device_type", d => d.DeviceType)
        .Add("model", d => d.Model)
        .Add("manufacturer", d => d.Manufacturer)
        .Add("serial_no", d => d.SerialNo)
        .Add("ip_address", d => d.IpAddress)
        .Add("location_code", d => d.LocationCode)
        .Add("is_active", d => d.IsActive)
        .Add("remark_1", d => d.Remark1)
        .Add("remark_2", d => d.Remark2)
        .Add("remark_3", d => d.Remark3)
        .Add("remark_4", d => d.Remark4)
        .Add("remark_5", d => d.Remark5)
        .Add("created_at", d => d.CreatedAt)
        .Add("updated_at", d => d.UpdatedAt)
        .Default("device_code")
        .ThenAlwaysBy(d => d.DeviceCode);

    private SortMap<Device>? _sort;

    public async Task<ServiceResult> ListAsync(
        CancellationToken ct, int? page = null, int? pageSize = null, string? q = null, string? sort = null,
        bool? isActive = null, string? deviceType = null, string? updateStatus = null)
    {
        var query = db.Devices.AsNoTracking().Where(d => !d.IsDeleted);
        if (isActive ?? true) query = query.Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(updateStatus) && updateStatus != "all" && statusSource is not null)
        {
            var codes = (await statusSource.DeviceCodesWithStatusAsync(updateStatus.Trim(), ct)).ToList();
            query = query.Where(d => codes.Contains(d.DeviceCode));
        }
        if (!string.IsNullOrWhiteSpace(deviceType) && Enum.TryParse<DeviceType>(deviceType, true, out var dt))
            query = query.Where(d => d.DeviceType == dt);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(d => EF.Functions.Like(d.DeviceCode, LikeContains(s), LikeEscape)
                                  || EF.Functions.Like(d.DeviceName, LikeContains(s), LikeEscape));
        }
        query = Sort.Apply(query, sort);

        var total = await query.CountAsync(ct);
        var pg = NormalizePage(page); var sz = NormalizePageSize(pageSize);
        var rows = await query.Skip((pg - 1) * sz).Take(sz).Select(d => Map(d)).ToListAsync(ct);
        return Outcome.Ok(new ApiListSuccess<DeviceDto>(rows, new PageMeta(total, pg, sz)));
    }

    public async Task<ServiceResult> GetAsync(string code, CancellationToken ct)
    {
        var d = await db.Devices.AsNoTracking().FirstOrDefaultAsync(x => x.DeviceCode == code && !x.IsDeleted, ct);
        return d is null
            ? Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Device")))
            : Outcome.Ok(new ApiSuccess<DeviceDto>(Map(d)));
    }

    public async Task<ServiceResult> CreateAsync(DeviceWriteDto body, CancellationToken ct)
    {
        if (ValidateCode(body.DeviceCode) is { } e1) return e1;
        if (ValidateName(body.DeviceName) is { } e2) return e2;
        if (ValidateIpAddress(body.IpAddress) is { } e3) return e3;
        if (!Enum.TryParse<DeviceType>(body.DeviceType, true, out var type))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var code = body.DeviceCode.Trim();
        if (await db.Devices.AnyAsync(d => d.DeviceCode == code, ct))
            return Outcome.Conflict(new ApiError(ErrorCatalog.Dup001()));
        var locationCode = NullIfBlank(body.LocationCode);
        if (locationCode is not null
            && !await db.Locations.AnyAsync(l => l.LocationCode == locationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        var d = new Device
        {
            DeviceCode = code,
            DeviceName = body.DeviceName.Trim(),
            DeviceType = type,
            Model = NullIfBlank(body.Model),
            Manufacturer = NullIfBlank(body.Manufacturer),
            Spec = NullIfBlank(body.Spec),
            SerialNo = NullIfBlank(body.SerialNo),
            IpAddress = NullIfBlank(body.IpAddress),
            LocationCode = locationCode,
            IsActive = body.IsActive,
            Remark1 = body.Remark1, Remark2 = body.Remark2, Remark3 = body.Remark3,
            Remark4 = body.Remark4, Remark5 = body.Remark5,
            CreatedAt = DateTime.Now,
        };
        db.Devices.Add(d);
        await db.SaveChangesAsync(ct);
        return Outcome.Created($"/masters/devices/{d.DeviceCode}", new ApiSuccess<DeviceDto>(Map(d)));
    }

    public async Task<ServiceResult> UpdateAsync(string code, DeviceWriteDto body, CancellationToken ct)
    {
        var d = await db.Devices.FirstOrDefaultAsync(x => x.DeviceCode == code && !x.IsDeleted, ct);
        if (d is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Device")));
        if (ValidateName(body.DeviceName) is { } e1) return e1;
        if (ValidateIpAddress(body.IpAddress) is { } e2) return e2;
        if (!Enum.TryParse<DeviceType>(body.DeviceType, true, out var type))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()));
        var locationCode = NullIfBlank(body.LocationCode);
        if (locationCode is not null
            && !await db.Locations.AnyAsync(l => l.LocationCode == locationCode && !l.IsDeleted, ct))
            return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Location")));

        d.DeviceName = body.DeviceName.Trim();
        d.DeviceType = type;
        d.Model = NullIfBlank(body.Model);
        d.Manufacturer = NullIfBlank(body.Manufacturer);
        d.Spec = NullIfBlank(body.Spec);
        d.SerialNo = NullIfBlank(body.SerialNo);
        d.IpAddress = NullIfBlank(body.IpAddress);
        d.LocationCode = locationCode;
        d.IsActive = body.IsActive;
        d.Remark1 = body.Remark1; d.Remark2 = body.Remark2; d.Remark3 = body.Remark3;
        d.Remark4 = body.Remark4; d.Remark5 = body.Remark5;
        d.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok(new ApiSuccess<DeviceDto>(Map(d)));
    }

    public async Task<ServiceResult> DeleteAsync(string code, CancellationToken ct)
    {
        var d = await db.Devices.FirstOrDefaultAsync(x => x.DeviceCode == code && !x.IsDeleted, ct);
        if (d is null) return Outcome.NotFound(new ApiError(ErrorCatalog.NotFound("Device")));
        d.IsDeleted = true; d.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(ct);
        return Outcome.NoContent();
    }

    public async Task<ServiceResult> ExportCsvAsync(CancellationToken ct)
    {
        var rows = await db.Devices.AsNoTracking().Where(d => !d.IsDeleted).OrderBy(d => d.DeviceCode).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("device_code,device_name,device_type,model,manufacturer,spec,serial_no,ip_address,location_code,is_active,remark_1,remark_2,remark_3,remark_4,remark_5");
        foreach (var d in rows)
            sb.Append(Csv(d.DeviceCode)).Append(',').Append(Csv(d.DeviceName)).Append(',')
              .Append(d.DeviceType.ToString()).Append(',').Append(Csv(d.Model)).Append(',')
              .Append(Csv(d.Manufacturer)).Append(',').Append(Csv(d.Spec)).Append(',')
              .Append(Csv(d.SerialNo)).Append(',').Append(Csv(d.IpAddress)).Append(',')
              .Append(Csv(d.LocationCode)).Append(',').Append(d.IsActive ? "1" : "0").Append(',')
              .Append(Csv(d.Remark1)).Append(',').Append(Csv(d.Remark2)).Append(',')
              .Append(Csv(d.Remark3)).Append(',').Append(Csv(d.Remark4)).Append(',')
              .Append(Csv(d.Remark5)).AppendLine();
        return CsvFile(sb, "devices.csv");
    }

    internal static DeviceDto Map(Device d) => new(
        d.DeviceCode, d.DeviceName, d.DeviceType.ToString(), d.Model, d.Manufacturer, d.Spec,
        d.SerialNo, d.IpAddress, d.LocationCode, d.IsActive,
        d.Remark1, d.Remark2, d.Remark3, d.Remark4, d.Remark5,
        d.CreatedAt, d.UpdatedAt);
}
