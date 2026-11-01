using Ims.Api.Contracts;
using System.Security.Claims;

namespace Ims.Api.Features.Import;

internal interface IImportService
{
    Task<ServiceResult> ImportReceivingScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ImportShippingScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ImportReturnsScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ImportPickingScheduleAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct);

    Task<ServiceResult> ImportProductionOrderAsync(IFormFile? file, ClaimsPrincipal user, CancellationToken ct);

    ServiceResult ReceivingTemplateAsync();

    ServiceResult ShippingTemplateAsync();

    ServiceResult ReturnsTemplateAsync();

    ServiceResult PickingTemplateAsync();
}
