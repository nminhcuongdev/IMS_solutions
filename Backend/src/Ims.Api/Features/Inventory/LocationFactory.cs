using Microsoft.EntityFrameworkCore;
using Ims.Infrastructure.Persistence;

namespace Ims.Api.Features.Inventory;

internal static class LocationFactory
{
    public static Task<string?> OfAsync(ImsDbContext db, string locationCode, CancellationToken ct) =>
        db.Locations.Where(l => l.LocationCode == locationCode && l.ZoneCode != null)
            .Join(db.Zones.Where(z => !z.IsDeleted), l => l.ZoneCode, z => z.ZoneCode, (l, z) => z.FactoryCode)
            .FirstOrDefaultAsync(ct);
}
