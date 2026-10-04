namespace Ims.Api.Features.Ht;

internal interface ILotLookup
{
    Task<bool> IsKnownLotAsync(string lotNo, CancellationToken ct);
}
