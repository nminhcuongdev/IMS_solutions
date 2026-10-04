using Ims.Api.Contracts;

namespace Ims.Api.Features.Menu;

internal interface IMenuService
{
    Task<ServiceResult> GetMenuAsync(HttpContext http, CancellationToken ct);
}
