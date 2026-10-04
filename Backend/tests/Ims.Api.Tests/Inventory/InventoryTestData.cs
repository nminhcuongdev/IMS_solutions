using System.Security.Claims;
using Ims.Api.Tests.Infrastructure;
using Ims.Domain.Masters;

namespace Ims.Api.Tests.Inventory;

internal static class InventoryTestData
{
    internal static readonly DateTime Now = new(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc);

    internal static ClaimsPrincipal User(string code = "admin") =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, code) }, "test"));

    internal static async Task SeedRefsAsync(TestDb t)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        t.Db.ItemGroups.Add(new ItemGroup
        {
            ItemGroupCode = "IG-LBL", ItemGroupName = "Label",
            Category = ItemGroupCategory.RawMaterials, IsActive = true, CreatedAt = now,
        });

        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-01", ItemName = "Industrial Printer 4in", ItemGroupCode = "IG-01",
            UomCode = "pcs", PackageQty = 1,
            ReorderLevel1 = 10, SurplusPoint = 60,
            IsActive = true, CreatedAt = now,
        });
        t.Db.Items.Add(new Item
        {
            ItemCode = "IT-02", ItemName = "Thermal Label Roll", ItemGroupCode = "IG-LBL",
            UomCode = "rolls", PackageQty = 1,
            ReorderLevel1 = 100, SurplusPoint = 900,
            IsActive = true, CreatedAt = now,
        });

        t.Db.Locations.Add(new Location
        {
            LocationCode = "W2", LocationType = LocationType.Large, LargeCode = "W2",
            LocationName = "Warehouse 2", ZoneCode = "ZONE-A", IsActive = true, CreatedAt = now,
        });
        t.Db.Locations.Add(new Location
        {
            LocationCode = "W2_ZB", LocationType = LocationType.Middle,
            LargeCode = "W2", MiddleCode = "ZB", LocationName = "W2 aisle B",
            ZoneCode = "ZONE-A", ParentLocationCode = "W2", IsActive = true, CreatedAt = now,
        });

        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-ADJ", ReasonName = "Cycle count discrepancy",
            ReasonCategoryValue = ReasonCategory.Adjustment, IsActive = true, CreatedAt = now,
        });
        t.Db.Reasons.Add(new Reason
        {
            ReasonCode = "RS-QC", ReasonName = "Quality inspection",
            ReasonCategoryValue = ReasonCategory.Hold, IsActive = true, CreatedAt = now,
        });

        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
    }

    internal static Ims.Domain.Inventory.Stock Stock(
        string item = "IT-01", string serial = "0000000001", decimal qty = 30,
        string? location = "W1_ZA", string? lot = "LOT260831",
        bool hold = false, string? reason = null, DateOnly? expiry = null,
        decimal reserved = 0) => new()
        {
            ItemCode = item, SerialNo = serial, FactoryCode = "FAC-01",
            LocationCode = location, LotNo = lot,
            StockQty = qty, ReservedQty = reserved,
            AvailableQty = hold ? 0 : qty - reserved,
            IsOnHold = hold, ReasonCode = reason, ExpiryDate = expiry,
            UpdatedAt = Now, UpdatedBy = "admin",
        };

    internal static async Task<TestDb> WithStockAsync(params Ims.Domain.Inventory.Stock[] rows)
    {
        var t = TestDb.Seeded();
        await SeedRefsAsync(t);
        t.Db.Stocks.AddRange(rows);
        await t.Db.SaveChangesAsync();
        t.Db.ChangeTracker.Clear();
        return t;
    }
}
