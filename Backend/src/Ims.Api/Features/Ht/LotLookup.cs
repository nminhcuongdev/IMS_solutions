using Microsoft.EntityFrameworkCore;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Ht;

internal sealed class LotLookup(ImsDbContext db) : ILotLookup
{
    public Task<bool> IsKnownLotAsync(string lotNo, CancellationToken ct) =>
        db.ReceivingPlans.AnyAsync(p => p.LotNo == lotNo, ct);
}
