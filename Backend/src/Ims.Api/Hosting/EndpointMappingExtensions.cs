using Ims.Api.Features.Alerts;
using Ims.Api.Features.Auth;
using Ims.Api.Features.Defaults;
using Ims.Api.Features.Devices;
using Ims.Api.Features.Distribution;
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
using Ims.Api.Features.Roles;
using Ims.Api.Features.Picking;
using Ims.Api.Features.Print;
using Ims.Api.Features.Processes;
using Ims.Api.Features.Production;
using Ims.Api.Features.Reasons;
using Ims.Api.Features.Receiving;
using Ims.Api.Features.Returns;
using Ims.Api.Features.Settings;
using Ims.Api.Features.Shipping;
using Ims.Api.Features.StockScan;
using Ims.Api.Features.Stocktaking;
using Ims.Api.Features.SuperAdmin;
using Ims.Api.Features.Traceability;
using Ims.Api.Features.Users;
using Ims.Api.Features.Zones;

namespace Ims.Api.Hosting;

internal static class EndpointMappingExtensions
{
    public static IEndpointRouteBuilder MapImsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        app.MapAuthEndpoints();
        app.MapDistributionDownloadEndpoints();
        var secured = app.MapGroup("");

        secured.MapMenuEndpoints();

        secured.MapDefaultValueEndpoints();
        secured.MapFactoryEndpoints();
        secured.MapZoneEndpoints();
        secured.MapDeviceEndpoints();
        secured.MapDistributionEndpoints();
        secured.MapLocationEndpoints();
        secured.MapItemGroupEndpoints();
        secured.MapItemEndpoints();
        secured.MapPartnerEndpoints();
        secured.MapReasonEndpoints();
        secured.MapProcessEndpoints();
        secured.MapProductionEndpoints();
        secured.MapUserEndpoints();

        secured.MapSettingsEndpoints();
        secured.MapPermissionEndpoints();
        secured.MapRoleEndpoints();
        secured.MapLanguageDataEndpoints();
        secured.MapHistoryEndpoints();
        secured.MapSuperAdminEndpoints();

        secured.MapReceivingEndpoints();
        secured.MapReturnsEndpoints();
        secured.MapShippingEndpoints();
        secured.MapPickingEndpoints();
        secured.MapInventoryEndpoints();
        secured.MapStocktakingEndpoints();
        secured.MapStockScanEndpoints();
        secured.MapAlertEndpoints();
        secured.MapImportEndpoints();
        secured.MapHtEndpoints();
        secured.MapPrintEndpoints();
        secured.MapTraceabilityEndpoints();
        return app;
    }
}

