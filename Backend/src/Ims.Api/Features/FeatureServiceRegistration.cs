using Ims.Api.Features.Alerts;
using Ims.Api.Features.Auth;
using Ims.Api.Features.Defaults;
using Ims.Api.Features.Devices;
using Ims.Api.Features.Factories;
using Ims.Api.Features.History;
using Ims.Api.Features.Ht;
using Ims.Api.Features.Import;
using Ims.Api.Features.Inventory;
using Ims.Api.Features.ItemGroups;
using Ims.Api.Features.Items;
using Ims.Api.Features.LanguageData;
using Ims.Api.Features.Locations;
using Ims.Api.Features.Menu;
using Ims.Api.Features.Partners;
using Ims.Api.Features.Permissions;
using Ims.Api.Features.Picking;
using Ims.Api.Features.Print;
using Ims.Api.Features.Reasons;
using Ims.Api.Features.Receiving;
using Ims.Api.Features.Returns;
using Ims.Api.Features.Settings;
using Ims.Api.Features.Shipping;
using Ims.Api.Features.StockScan;
using Ims.Api.Features.Stocktaking;
using Ims.Api.Features.SuperAdmin;
using Ims.Api.Features.Users;
using Ims.Api.Features.Zones;

namespace Ims.Api.Features;

public static class FeatureServiceRegistration
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDefaultValueService, DefaultValueService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IFactoryService, FactoryService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IHtService, HtService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IItemGroupService, ItemGroupService>();
        services.AddScoped<ILanguageDataService, LanguageDataService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPickingService, PickingService>();
        services.AddScoped<IPrintService, PrintService>();
        services.AddScoped<IReasonService, ReasonService>();
        services.AddScoped<IReceivingService, ReceivingService>();
        services.AddScoped<IReturnsService, ReturnsService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IStocktakingService, StocktakingService>();
        services.AddScoped<IStockScanService, StockScanService>();
        services.AddScoped<ISuperAdminService, SuperAdminService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IPrintTargetResolver, PrintTargetResolver>();
        services.AddScoped<ILotLookup, LotLookup>();
        return services;
    }
}
