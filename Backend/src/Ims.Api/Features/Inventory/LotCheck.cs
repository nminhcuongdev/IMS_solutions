using Ims.Api.Contracts;

namespace Ims.Api.Features.Inventory;

internal sealed record LotCheck(string? Lot, ApiErrorBody? Error);
