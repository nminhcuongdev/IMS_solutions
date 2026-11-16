using Ims.Api.Contracts;

namespace Ims.Api.Features.LanguageData;

public interface IMessageCatalog
{
    Task<ApiErrorBody> LocalizeAsync(ApiErrorBody body, HttpContext http, CancellationToken ct);
}
