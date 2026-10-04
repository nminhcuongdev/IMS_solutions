using Microsoft.EntityFrameworkCore;
using Ims.Domain.Masters;
using Ims.Domain.History;
using Ims.Domain.System;
using Ims.Domain.Receiving;

namespace Ims.Infrastructure.Persistence;

public class ImsDbContext : DbContext
{
    public ImsDbContext(DbContextOptions<ImsDbContext> options) : base(options) { }

    public DbSet<Factory> Factories => Set<Factory>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ItemGroup> ItemGroups => Set<ItemGroup>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Reason> Reasons => Set<Reason>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<StatusMaster> Statuses => Set<StatusMaster>();
    public DbSet<DefaultValue> DefaultValues => Set<DefaultValue>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<LanguageData> LanguageData => Set<LanguageData>();
    public DbSet<FunctionLarge> FunctionLarges => Set<FunctionLarge>();
    public DbSet<AppFunction> AppFunctions => Set<AppFunction>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<FunctionVisibility> FunctionVisibilities => Set<FunctionVisibility>();
    public DbSet<GeneralSetting> GeneralSettings => Set<GeneralSetting>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    public DbSet<AlertConfig> AlertConfigs => Set<AlertConfig>();

    public DbSet<PrintHistory> PrintHistories => Set<PrintHistory>();
    public DbSet<RfidGateHistory> RfidGateHistories => Set<RfidGateHistory>();

    public DbSet<ReceivingPlan> ReceivingPlans => Set<ReceivingPlan>();
    public DbSet<ReceivingTemp> ReceivingTemps => Set<ReceivingTemp>();
    public DbSet<ReceivingResult> ReceivingResults => Set<ReceivingResult>();

    public DbSet<Ims.Domain.Returns.ReturnPlan> ReturnPlans => Set<Ims.Domain.Returns.ReturnPlan>();
    public DbSet<Ims.Domain.Returns.ReturnResult> ReturnResults => Set<Ims.Domain.Returns.ReturnResult>();

    public DbSet<Ims.Domain.Shipping.ShippingPlan> ShippingPlans => Set<Ims.Domain.Shipping.ShippingPlan>();
    public DbSet<Ims.Domain.Shipping.ShippingResult> ShippingResults => Set<Ims.Domain.Shipping.ShippingResult>();
    public DbSet<Ims.Domain.Shipping.ShippingTemp> ShippingTemps => Set<Ims.Domain.Shipping.ShippingTemp>();
    public DbSet<Ims.Domain.Picking.PickingPlan> PickingPlans => Set<Ims.Domain.Picking.PickingPlan>();
    public DbSet<Ims.Domain.Picking.PickingResult> PickingResults => Set<Ims.Domain.Picking.PickingResult>();

    public DbSet<Ims.Domain.Inventory.Stock> Stocks => Set<Ims.Domain.Inventory.Stock>();
    public DbSet<Ims.Domain.Inventory.InventoryHistory> InventoryHistories => Set<Ims.Domain.Inventory.InventoryHistory>();
    public DbSet<Ims.Domain.Inventory.LocationTransfer> LocationTransfers => Set<Ims.Domain.Inventory.LocationTransfer>();

    public DbSet<Ims.Domain.Stocktaking.StocktakingSession> StocktakingSessions => Set<Ims.Domain.Stocktaking.StocktakingSession>();
    public DbSet<Ims.Domain.Stocktaking.StocktakingLabel> StocktakingLabels => Set<Ims.Domain.Stocktaking.StocktakingLabel>();
    public DbSet<Ims.Domain.Stocktaking.StocktakingHistory> StocktakingHistories => Set<Ims.Domain.Stocktaking.StocktakingHistory>();
    public DbSet<Ims.Domain.Stocktaking.StockScan> StockScans => Set<Ims.Domain.Stocktaking.StockScan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(ImsDbContext).Assembly);
    }
}
