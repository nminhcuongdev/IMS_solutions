using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ims.Api.Hosting;
using Ims.Domain.System;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Contracts;

internal static class RequestReplays
{
    public const int MaxLength = 50;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new LocalDateTimeJsonConverter() },
    };

    public static string? Normalize(string? requestId) =>
        string.IsNullOrWhiteSpace(requestId) ? null : requestId.Trim();

    public static ServiceResult? Invalid(string? requestId) =>
        requestId is { Length: > MaxLength }
            ? Outcome.BadRequest(new ApiError(ErrorCatalog.Val005TooLong(requestId, MaxLength)))
            : null;

    public static async Task<ServiceResult?> FindAsync(ImsDbContext db, string? requestId, CancellationToken ct)
    {
        if (requestId is null) return null;
        var row = await db.RequestReplays.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == requestId, ct);
        if (row is null) return null;
        using var doc = JsonDocument.Parse(row.ResponseJson);
        return new ServiceResult(row.StatusCode, doc.RootElement.Clone(), row.Location);
    }

    public static void Record(ImsDbContext db, string? requestId, string kind, ServiceResult result, string? who)
    {
        if (requestId is null || result.StatusCode >= 300) return;
        db.RequestReplays.Add(new RequestReplay
        {
            RequestId = requestId,
            Kind = kind,
            StatusCode = result.StatusCode,
            Location = result.Location,
            ResponseJson = JsonSerializer.Serialize(result.Body, Json),
            CreatedAt = DateTime.Now,
            CreatedBy = who,
        });
    }
}
