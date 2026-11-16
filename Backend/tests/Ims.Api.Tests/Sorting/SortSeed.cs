using Ims.Api.Tests.Infrastructure;
using Ims.Domain.History;
using Ims.Domain.Inventory;
using Ims.Domain.Masters;
using Ims.Domain.Picking;
using Ims.Domain.Receiving;
using Ims.Domain.Returns;
using Ims.Domain.Shipping;
using Ims.Domain.System;

namespace Ims.Api.Tests.Sorting;

internal static class SortSeed
{
    private static readonly DateTime Now = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    public static TestDb Masters()
    {
        var t = TestDb.Seeded();
        var db = t.Db;

        db.ItemGroups.Add(new ItemGroup
        {
            ItemGroupCode = "IG-02", ItemGroupName = "Another group",
            Category = ItemGroupCategory.RawMaterials, IsActive = true, CreatedAt = Now,
        });
        db.DefaultValues.Add(new DefaultValue
        {
            ValueType = "PackingType", ValueCode = "CASE",
            NameLang1 = "Case", NameLang2 = "Case", NameLang3 = "Case", IsActive = true, CreatedAt = Now,
        });
        db.Items.Add(new Item
        {
            ItemCode = "IT-1", ItemName = "Zeta", ItemGroupCode = "IG-01", UomCode = "pcs",
            PackingTypeCode = "BOX", PackageQty = 1, ReorderLevel1 = 1, IsActive = true, CreatedAt = Now,
        });
        db.Items.Add(new Item
        {
            ItemCode = "IT-2", ItemName = "Alpha", ItemGroupCode = "IG-02", UomCode = "box",
            PackingTypeCode = "CASE", PackageQty = 2, ReorderLevel1 = 2, IsActive = true, CreatedAt = Now,
        });
        db.Partners.Add(new Partner
        {
            PartnerCode = "BP1", PartnerName = "Partner 1", IsSupplier = true, IsCustomer = true,
            IsActive = true, CreatedAt = Now,
        });
        db.Partners.Add(new Partner
        {
            PartnerCode = "BP2", PartnerName = "Partner 2", IsSupplier = true, IsCustomer = true,
            IsActive = true, CreatedAt = Now,
        });
        db.AppUsers.Add(new AppUser
        {
            UserCode = "U1", UserName = "Zed", PasswordHash = "x", RoleCode = "ADMIN", IsActive = true,
            CreatedAt = Now,
        });
        db.AppUsers.Add(new AppUser
        {
            UserCode = "U2", UserName = "Amy", PasswordHash = "x", RoleCode = "ADMIN", IsActive = true,
            CreatedAt = Now,
        });
        db.Reasons.Add(new Reason
        {
            ReasonCode = "R1", ReasonName = "Zzz", ReasonCategoryValue = ReasonCategory.Shortage,
            IsActive = true, CreatedAt = Now,
        });
        db.Reasons.Add(new Reason
        {
            ReasonCode = "R2", ReasonName = "Aaa", ReasonCategoryValue = ReasonCategory.Shortage,
            IsActive = true, CreatedAt = Now,
        });
        db.Locations.Add(new Location
        {
            LocationCode = "W2", LocationType = LocationType.Large, LargeCode = "W2",
            LocationName = "Warehouse 2", ZoneCode = "ZONE-A", IsActive = true, CreatedAt = Now,
        });
        db.Locations.Add(new Location
        {
            LocationCode = "W2_ZB", LocationType = LocationType.Middle, LargeCode = "W2", MiddleCode = "ZB",
            LocationName = "W2 aisle", ZoneCode = "ZONE-A", ParentLocationCode = "W2", IsActive = true,
            CreatedAt = Now,
        });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return t;
    }

    public static TestDb Receiving()
    {
        var t = Masters();
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "PO-1", PoBranchNo = "01", PartnerCode = "BP1", ItemCode = "IT-1", Qty = 5, ActualQty = 1,
            ScheduledDate = new DateOnly(2026, 9, 2), Status = ReceivingStatus.Partial, ReasonCode = "R2",
            InvoiceNo = "INV-2", CreatedAt = Now, CreatedBy = "U2",
        });
        t.Db.ReceivingPlans.Add(new ReceivingPlan
        {
            PoNo = "PO-2", PoBranchNo = "02", PartnerCode = "BP2", ItemCode = "IT-2", Qty = 8, ActualQty = 3,
            ScheduledDate = new DateOnly(2026, 9, 1), Status = ReceivingStatus.Cancelled, ReasonCode = "R1",
            InvoiceNo = "INV-1", CreatedAt = Now.AddHours(1), CreatedBy = "U1",
        });
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = "PO-1", PoBranchNo = "01", ItemCode = "IT-1", Qty = 5, LocationCode = "W1_ZA",
            InvoiceNo = "INV-2", ExpiryDate = new DateOnly(2026, 12, 9), CreatedAt = Now, CreatedBy = "U1",
        });
        t.Db.ReceivingResults.Add(new ReceivingResult
        {
            PoNo = "PO-2", PoBranchNo = "02", ItemCode = "IT-2", Qty = 3, LocationCode = "W2_ZB",
            InvoiceNo = "INV-1", ExpiryDate = new DateOnly(2026, 12, 8), CreatedAt = Now.AddHours(1),
            CreatedBy = "U2",
        });
        return Save(t);
    }

    public static TestDb Shipping()
    {
        var t = Masters();
        t.Db.ShippingPlans.Add(new ShippingPlan
        {
            SoNo = "SO-1", SoBranchNo = "01", PartnerCode = "BP1", ItemCode = "IT-1", Qty = 5, ActualQty = 1,
            ScheduledDate = new DateOnly(2026, 9, 2), Status = ShippingStatus.Partial, ReasonCode = "R2",
            InvoiceNo = "INV-2", CreatedAt = Now, CreatedBy = "U2",
        });
        t.Db.ShippingPlans.Add(new ShippingPlan
        {
            SoNo = "SO-2", SoBranchNo = "02", PartnerCode = "BP2", ItemCode = "IT-2", Qty = 8, ActualQty = 3,
            ScheduledDate = new DateOnly(2026, 9, 1), Status = ShippingStatus.Cancelled, ReasonCode = "R1",
            InvoiceNo = "INV-1", CreatedAt = Now.AddHours(1), CreatedBy = "U1",
        });
        t.Db.ShippingResults.Add(new ShippingResult
        {
            SoNo = "SO-1", SoBranchNo = "01", ItemCode = "IT-1", Qty = 5, LocationCode = "W1_ZA",
            ExpiryDate = new DateOnly(2026, 12, 9), CreatedAt = Now, CreatedBy = "U1",
        });
        t.Db.ShippingResults.Add(new ShippingResult
        {
            SoNo = "SO-2", SoBranchNo = "02", ItemCode = "IT-2", Qty = 3, LocationCode = "W2_ZB",
            ExpiryDate = new DateOnly(2026, 12, 8), CreatedAt = Now.AddHours(1), CreatedBy = "U2",
        });
        return Save(t);
    }

    public static TestDb Returns()
    {
        var t = Masters();
        t.Db.ReturnPlans.Add(new ReturnPlan
        {
            ReturnNo = "RTN-1", ReturnBranchNo = "01", PartnerCode = "BP1", ItemCode = "IT-1", Qty = 5,
            ActualQty = 1, ScheduledDate = new DateOnly(2026, 9, 2), Status = ReturnStatus.Partial,
            ReasonCode = "R2", FactoryCode = "FAC-01", CreatedAt = Now, CreatedBy = "U2",
        });
        t.Db.ReturnPlans.Add(new ReturnPlan
        {
            ReturnNo = "RTN-2", ReturnBranchNo = "02", PartnerCode = "BP2", ItemCode = "IT-2", Qty = 8,
            ActualQty = 3, ScheduledDate = new DateOnly(2026, 9, 1), Status = ReturnStatus.Cancelled,
            ReasonCode = "R1", FactoryCode = "FAC-01", CreatedAt = Now.AddHours(1), CreatedBy = "U1",
        });
        return Save(t);
    }

    public static TestDb Picking()
    {
        var t = Masters();
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-1", SoBranchNo = "01", ItemCode = "IT-1", LocationCode = "W1_ZA", Qty = 5,
            ScheduledDate = new DateOnly(2026, 9, 2), CreatedAt = Now, CreatedBy = "U2",
        });
        t.Db.PickingPlans.Add(new PickingPlan
        {
            SoNo = "SO-2", SoBranchNo = "02", ItemCode = "IT-2", LocationCode = "W2_ZB", Qty = 8,
            ScheduledDate = new DateOnly(2026, 9, 1), CreatedAt = Now.AddHours(1), CreatedBy = "U1",
        });
        t.Db.PickingResults.Add(new PickingResult
        {
            SoNo = "SO-1", SoBranchNo = "01", ItemCode = "IT-1", Qty = 5, LocationCode = "W1_ZA",
            CreatedAt = Now, CreatedBy = "U1",
        });
        t.Db.PickingResults.Add(new PickingResult
        {
            SoNo = "SO-2", SoBranchNo = "02", ItemCode = "IT-2", Qty = 3, LocationCode = "W2_ZB",
            CreatedAt = Now.AddHours(1), CreatedBy = "U2",
        });
        return Save(t);
    }

    public static TestDb History()
    {
        var t = Masters();
        t.Db.PrintHistories.Add(new PrintHistory
        {
            Category = "Receiving", CreatedAt = Now, DocNo = "PO-1", ItemCode = "IT-1", Qty = 1, Status = 14,
        });
        t.Db.PrintHistories.Add(new PrintHistory
        {
            Category = "Receiving", CreatedAt = Now, DocNo = "PO-2", ItemCode = "IT-2", Qty = 1, Status = 14,
        });
        t.Db.RfidGateHistories.Add(new RfidGateHistory { CreatedAt = Now, GateCode = "G1", ItemCode = "IT-1" });
        t.Db.RfidGateHistories.Add(new RfidGateHistory { CreatedAt = Now, GateCode = "G1", ItemCode = "IT-2" });
        return Save(t);
    }

    public static TestDb Stock()
    {
        var t = Masters();
        t.Db.Stocks.Add(new Stock
        {
            ItemCode = "IT-1", SerialNo = "S-1", FactoryCode = "FAC-01", LocationCode = "W1_ZA", LotNo = "L1",
            StockQty = 10, AvailableQty = 10, IsOnHold = false, ExpiryDate = new DateOnly(2027, 1, 1),
            UpdatedAt = Now,
        });
        t.Db.Stocks.Add(new Stock
        {
            ItemCode = "IT-2", SerialNo = "S-2", FactoryCode = "FAC-01", LocationCode = "W2_ZB", LotNo = "L2",
            StockQty = 20, AvailableQty = 0, IsOnHold = true, ReasonCode = "R1",
            ExpiryDate = new DateOnly(2026, 12, 1), UpdatedAt = Now,
        });
        t.Db.Stocks.Add(new Stock
        {
            ItemCode = "IT-1", SerialNo = "S-3", FactoryCode = "FAC-01", LocationCode = "W2_ZB", LotNo = "L3",
            StockQty = 1, AvailableQty = 1, UpdatedAt = Now,
        });
        t.Db.InventoryHistories.Add(new InventoryHistory
        {
            CreatedAt = Now, ItemCode = "IT-1", LocationCode = "W2_ZB", LotNo = "L2", ActionType = "Receiving",
            Qty = 1, ReasonCode = "R1", CreatedBy = "U1",
        });
        t.Db.InventoryHistories.Add(new InventoryHistory
        {
            CreatedAt = Now, ItemCode = "IT-2", LocationCode = "W1_ZA", LotNo = "L1", ActionType = "Receiving",
            Qty = 1, ReasonCode = "R2", CreatedBy = "U2",
        });
        return Save(t);
    }

    private static TestDb Save(TestDb t)
    {
        t.Db.SaveChanges();
        t.Db.ChangeTracker.Clear();
        return t;
    }
}
