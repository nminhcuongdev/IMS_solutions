using System.Security.Claims;
using Ims.Api.Contracts;

namespace Ims.Api.Features.Print;

internal interface IPrintService
{
    Task<ServiceResult> PreviewAsync(PrintPreviewRequest body, CancellationToken ct);

    Task<ServiceResult> LabelAsync(PrintLabelRequest body, ClaimsPrincipal user, CancellationToken ct);
}
