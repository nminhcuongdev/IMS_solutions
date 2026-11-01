using Ims.Api.Contracts;

namespace Ims.Api.Features.LanguageData;

internal interface ILanguageDataService
{
    Task<ServiceResult> ListAsync(
        CancellationToken ct, string? q = null, string? labelKey = null, string? deviceScope = null,
        string? sort = null, int? page = null, int? pageSize = null);

    Task<ServiceResult> SaveAsync(LanguageSaveDto body, CancellationToken ct);

    Task<ServiceResult> LabelsAsync(CancellationToken ct, string? lang = null, string deviceScope = "PC");

    Task<ServiceResult> LanguagesAsync(CancellationToken ct);

    Task<ServiceResult> ExportCsvAsync(
        CancellationToken ct, string? q = null, string? labelKey = null, string? deviceScope = null,
        string? sort = null);
}
