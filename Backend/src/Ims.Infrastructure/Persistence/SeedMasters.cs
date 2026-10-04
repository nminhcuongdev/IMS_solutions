using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.Masters;

namespace Ims.Infrastructure.Persistence;

internal static class SeedMasters
{
    public sealed class MasterSeed
    {
        public List<FactoryRow> Factories { get; set; } = new();
        public List<ZoneRow> Zones { get; set; } = new();
        public List<LocationRow> Locations { get; set; } = new();
        public List<ItemGroupRow> ItemGroups { get; set; } = new();
        public List<ItemRow> Items { get; set; } = new();
        public List<PartnerRow> Partners { get; set; } = new();
        public List<UserRow> Users { get; set; } = new();
        public List<ReasonRow> Reasons { get; set; } = new();
    }

    public abstract class Remarks
    {
        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }
        public string? Remark3 { get; set; }
        public string? Remark4 { get; set; }
        public string? Remark5 { get; set; }
    }

    public sealed class FactoryRow : Remarks
    {
        public string FactoryCode { get; set; } = null!;
        public string FactoryName { get; set; } = null!;
        public string? Country { get; set; }
        public string? Address { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ZoneRow : Remarks
    {
        public string ZoneCode { get; set; } = null!;
        public string ZoneName { get; set; } = null!;
        public string FactoryCode { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public sealed class LocationRow : Remarks
    {
        public string LocationCode { get; set; } = null!;
        public string LocationType { get; set; } = null!;
        public string LargeCode { get; set; } = null!;
        public string? MiddleCode { get; set; }
        public string? SmallCode { get; set; }
        public string LocationName { get; set; } = null!;
        public string? ZoneCode { get; set; }
        public string? ParentLocationCode { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ItemGroupRow : Remarks
    {
        public string ItemGroupCode { get; set; } = null!;
        public string ItemGroupName { get; set; } = null!;
        public int Category { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ItemRow : Remarks
    {
        public string ItemCode { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public string ItemGroupCode { get; set; } = null!;
        public string? UomCode { get; set; }
        public string? PackingTypeCode { get; set; }
        public decimal PackageQty { get; set; } = 1;
        public int? ExpirationPeriod { get; set; }
        public string? Barcode { get; set; }
        public decimal? SurplusPoint { get; set; }
        public decimal? ReorderLevel1 { get; set; }
        public decimal? ReorderLevel2 { get; set; }
        public string? DefaultLocationCode { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class PartnerRow : Remarks
    {
        public string PartnerCode { get; set; } = null!;
        public string PartnerName { get; set; } = null!;
        public bool IsSupplier { get; set; }
        public bool IsCustomer { get; set; }
        public string? Country { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class UserRow : Remarks
    {
        public string UserCode { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string RoleCode { get; set; } = null!;
        public string? DefaultLanguage { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class ReasonRow : Remarks
    {
        public string ReasonCode { get; set; } = null!;
        public string ReasonName { get; set; } = null!;
        public string ReasonCategory { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    private static MasterSeed? _cached;

    public static MasterSeed? Load()
    {
        if (_cached is not null) return _cached;
        var asm = typeof(SeedMasters).Assembly;
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith("masters.json", StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;
        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null) return null;
        _cached = JsonSerializer.Deserialize<MasterSeed>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        });
        return _cached;
    }

    public static async Task RunAsync(ImsDbContext db, CancellationToken ct)
    {
        var seed = Load();
        if (seed is null) return;

        await SeedFactories(db, seed, ct);
        await SeedZones(db, seed, ct);
        await SeedLocations(db, seed, ct);
        await SeedItemGroups(db, seed, ct);
        await SeedItems(db, seed, ct);
        await SeedPartners(db, seed, ct);
        await SeedReasons(db, seed, ct);
    }

    private static async Task SeedFactories(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.Factories.Select(x => x.FactoryCode).ToListAsync(ct);
        foreach (var r in s.Factories.Where(r => !have.Contains(r.FactoryCode)))
            db.Factories.Add(new Factory
            {
                FactoryCode = r.FactoryCode, FactoryName = r.FactoryName,
                Country = r.Country, Address = r.Address,
                IsDefault = r.IsDefault, IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedZones(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.Zones.Select(x => x.ZoneCode).ToListAsync(ct);
        var factories = await db.Factories.Select(x => x.FactoryCode).ToListAsync(ct);
        foreach (var r in s.Zones.Where(r => !have.Contains(r.ZoneCode) && factories.Contains(r.FactoryCode)))
            db.Zones.Add(new Zone
            {
                ZoneCode = r.ZoneCode, ZoneName = r.ZoneName, FactoryCode = r.FactoryCode,
                IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedLocations(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.Locations.Select(x => x.LocationCode).ToListAsync(ct);
        var zones = await db.Zones.Select(x => x.ZoneCode).ToListAsync(ct);

        foreach (var level in new[] { "Large", "Middle", "Small" })
        {
            foreach (var r in s.Locations.Where(r => r.LocationType == level && !have.Contains(r.LocationCode)))
                db.Locations.Add(new Location
                {
                    LocationCode = r.LocationCode,
                    LocationType = Enum.Parse<LocationType>(r.LocationType),
                    LargeCode = r.LargeCode, MiddleCode = r.MiddleCode, SmallCode = r.SmallCode,
                    LocationName = r.LocationName,
                    ZoneCode = r.ZoneCode is not null && zones.Contains(r.ZoneCode) ? r.ZoneCode : null,
                    ParentLocationCode = r.ParentLocationCode,
                    IsActive = r.IsActive,
                    Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                    Remark4 = r.Remark4, Remark5 = r.Remark5,
                    CreatedAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedItemGroups(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.ItemGroups.Select(x => x.ItemGroupCode).ToListAsync(ct);
        foreach (var r in s.ItemGroups.Where(r => !have.Contains(r.ItemGroupCode)))
            db.ItemGroups.Add(new ItemGroup
            {
                ItemGroupCode = r.ItemGroupCode, ItemGroupName = r.ItemGroupName,
                Category = (ItemGroupCategory)r.Category, IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedItems(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.Items.Select(x => x.ItemCode).ToListAsync(ct);
        var groups = await db.ItemGroups.Select(x => x.ItemGroupCode).ToListAsync(ct);
        var locs = await db.Locations.Select(x => x.LocationCode).ToListAsync(ct);
        foreach (var r in s.Items.Where(r => !have.Contains(r.ItemCode) && groups.Contains(r.ItemGroupCode)))
            db.Items.Add(new Item
            {
                ItemCode = r.ItemCode, ItemName = r.ItemName, ItemGroupCode = r.ItemGroupCode,
                UomCode = r.UomCode, PackingTypeCode = r.PackingTypeCode,
                PackageQty = r.PackageQty, ExpirationPeriod = r.ExpirationPeriod,
                Barcode = r.Barcode, SurplusPoint = r.SurplusPoint,
                ReorderLevel1 = r.ReorderLevel1, ReorderLevel2 = r.ReorderLevel2,
                DefaultLocationCode = r.DefaultLocationCode is not null && locs.Contains(r.DefaultLocationCode)
                    ? r.DefaultLocationCode : null,
                IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPartners(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.Partners.Select(x => x.PartnerCode).ToListAsync(ct);
        foreach (var r in s.Partners.Where(r => !have.Contains(r.PartnerCode)))
            db.Partners.Add(new Partner
            {
                PartnerCode = r.PartnerCode, PartnerName = r.PartnerName,
                IsSupplier = r.IsSupplier, IsCustomer = r.IsCustomer,
                Country = r.Country, Address = r.Address, IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedReasons(ImsDbContext db, MasterSeed s, CancellationToken ct)
    {
        var have = await db.Reasons.Select(x => x.ReasonCode).ToListAsync(ct);
        foreach (var r in s.Reasons.Where(r => !have.Contains(r.ReasonCode)))
            db.Reasons.Add(new Reason
            {
                ReasonCode = r.ReasonCode, ReasonName = r.ReasonName,
                ReasonCategoryValue = Enum.Parse<ReasonCategory>(r.ReasonCategory),
                IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync(ct);
    }
}
