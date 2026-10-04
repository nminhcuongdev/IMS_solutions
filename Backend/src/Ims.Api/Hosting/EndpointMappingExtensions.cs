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

namespace Ims.Api.Hosting;

internal static class EndpointMappingExtensions
{
    public static IEndpointRouteBuilder MapImsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        app.MapAuthEndpoints();
        app.MapMenuEndpoints();

        app.MapDefaultValueEndpoints();
        app.MapFactoryEndpoints();
        app.MapZoneEndpoints();
        app.MapDeviceEndpoints();
        app.MapLocationEndpoints();
        app.MapItemGroupEndpoints();
        app.MapItemEndpoints();
        app.MapPartnerEndpoints();
        app.MapReasonEndpoints();
        app.MapUserEndpoints();

        app.MapSettingsEndpoints();
        app.MapPermissionEndpoints();
        app.MapLanguageDataEndpoints();
        app.MapHistoryEndpoints();
        app.MapSuperAdminEndpoints();

        app.MapReceivingEndpoints();
        app.MapReturnsEndpoints();
        app.MapShippingEndpoints();
        app.MapPickingEndpoints();
        app.MapInventoryEndpoints();
        app.MapStocktakingEndpoints();
        app.MapStockScanEndpoints();
        app.MapAlertEndpoints();
        app.MapImportEndpoints();
        app.MapHtEndpoints();
        app.MapPrintEndpoints();
        return app;
    }
}
