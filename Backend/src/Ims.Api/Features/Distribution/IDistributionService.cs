using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Distribution;

internal interface IDistributionService
{
    Task<ServiceResult> UploadPackageAsync(string fileName, byte[] bytes, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListPackagesAsync(CancellationToken ct);

    Task<ServiceResult> DeletePackageAsync(int id, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> AttributesAsync(CancellationToken ct);

    Task<ServiceResult> PreviewTargetsAsync(TargetQueryDto body, CancellationToken ct);

    Task<ServiceResult> CreateRuleAsync(RuleWriteDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListRulesAsync(CancellationToken ct);

    Task<ServiceResult> GetRuleAsync(int id, CancellationToken ct);

    Task<ServiceResult> StopRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ResumeRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> RetryRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> DeleteRuleAsync(int id, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ListDevicesAsync(string? status, CancellationToken ct);

    Task<ServiceResult> DeviceHistoryAsync(string code, CancellationToken ct);

    Task<ServiceResult> ListUnknownAsync(CancellationToken ct);

    Task<ServiceResult> ListAuditAsync(CancellationToken ct);

    Task<ServiceResult> GetSettingsAsync(CancellationToken ct);

    Task<ServiceResult> SaveSettingsAsync(DistSettingsDto body, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> VersionJsonPreviewAsync(CancellationToken ct);

    Task<bool> TickAsync(CancellationToken ct);
}
