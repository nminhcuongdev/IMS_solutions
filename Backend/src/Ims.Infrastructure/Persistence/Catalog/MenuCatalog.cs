namespace Ims.Infrastructure.Persistence.Catalog;

public static class MenuCatalog
{
    public const string Open = "Open";
    public const string AdminRole = "ADMIN";
    public const string GeneralRole = "GENERAL";
    public const string WorkerRole = "WORKER";
    public const string ModulePrefix = "module_";

    public static readonly string[] Modules =
        ["receiving", "shipping", "picking", "returns", "stocktaking", "rfid", "production"];

    private static readonly string[] MasterActions = ["Open", "New", "Edit", "Copy", "Delete", "Import", "Export"];
    private static readonly string[] ScheduleActions = ["Open", "New", "StatusChange", "Label", "Shortage", "Import", "Export"];

    public static readonly CatalogGroup[] Groups =
    [
        new("menu", "Menu", 1),
        new("inventoryMgmt", "Inventory", 2),
        new("alertCenter", "Alert Center", 3),
        new("receivingMgmt", "Receiving", 4),
        new("shippingMgmt", "Shipping", 5),
        new("productionMgmt", "Production", 6),
        new("traceMgmt", "Traceability", 7),
        new("stocktakeMgmt", "Stocktaking", 8),
        new("printHistory", "Print History", 9),
        new("rfidHistory", "RFID History", 10),
        new("masterMgmt", "Master Data", 11),
        new("otherMgmt", "Other", 12),
        new("menu_wh", "Warehouse", 13),
        new("ht_production", "Production", 14),
        new("menu_cm", "Common", 15),
    ];

    public static readonly CatalogFunction[] Functions =
    [
        new("dashboard", "Menu", "menu", 1, null, FunctionKind.Menu, []),
        new("inv", "Stock Inquiry", "inventoryMgmt", 1, null, FunctionKind.Transaction, ["Open", "Adjust", "Disposal", "Export"]),
        new("inv_history", "Work History", "inventoryMgmt", 2, null, FunctionKind.Transaction, ["Open", "RestoreDisposal", "Export"]),
        new("inv_hold", "Hold Item", "inventoryMgmt", 3, null, FunctionKind.Transaction, ["Open", "Hold", "Release", "Export"]),
        new("alert_center", "Alert Center", "alertCenter", 1, null, FunctionKind.Transaction, ["Open"]),
        new("rcv_import", "Import Receiving Schedule", "receivingMgmt", 1, "receiving", FunctionKind.Transaction, ScheduleActions),
        new("returns", "Returns", "receivingMgmt", 2, "returns", FunctionKind.Transaction, ScheduleActions),
        new("rcv_result", "Receiving/Returns Result", "receivingMgmt", 3, "receiving", FunctionKind.Transaction, ["Open", "Reprint", "Export"]),
        new("pick_import", "Import Picking Schedule", "shippingMgmt", 1, "picking", FunctionKind.Transaction, ["Open", "New", "StatusChange", "Import", "Export"]),
        new("pick_result", "Picking Result", "shippingMgmt", 2, "picking", FunctionKind.Transaction, ["Open", "Export"]),
        new("shp_import", "Import Shipping Schedule", "shippingMgmt", 3, "shipping", FunctionKind.Transaction, ScheduleActions),
        new("shp_result", "Shipping Result", "shippingMgmt", 4, "shipping", FunctionKind.Transaction, ["Open", "Reprint", "Export"]),
        new("prd_import", "Production Order", "productionMgmt", 1, "production", FunctionKind.Transaction, ["Open", "StatusChange", "MaterialPicking", "Import", "Export"]),
        new("prd_result", "Production Result", "productionMgmt", 2, "production", FunctionKind.Transaction, ["Open", "Reverse", "Export"]),
        new("trace_back", "Trace Back", "traceMgmt", 1, "production", FunctionKind.Transaction, ["Open", "Export"]),
        new("trace_fwd", "Trace Forward", "traceMgmt", 2, "production", FunctionKind.Transaction, ["Open", "Export"]),
        new("st_label", "Item Stocktaking", "stocktakeMgmt", 1, "stocktaking", FunctionKind.Transaction, ["Open", "Start", "Complete", "Finalize", "Export"]),
        new("st_ref", "Stocktaking History", "stocktakeMgmt", 2, "stocktaking", FunctionKind.Transaction, ["Open", "Export"]),
        new("st_rfid_demo", "RFID Stock Count", "stocktakeMgmt", 3, "rfid", FunctionKind.Transaction, ["Open", "Reset", "SelectTarget", "Register", "Delete"]),
        new("print_history", "Print History", "printHistory", 1, null, FunctionKind.Transaction, ["Open", "Reprint", "Export"]),
        new("rfid_gate_history", "RFID Gate History", "rfidHistory", 1, "rfid", FunctionKind.Transaction, ["Open", "Export"]),
        new("m_factory", "Factory Master", "masterMgmt", 1, null, FunctionKind.Master, MasterActions),
        new("m_zone", "Zone Master", "masterMgmt", 2, null, FunctionKind.Master, MasterActions),
        new("m_location", "Location Master", "masterMgmt", 3, null, FunctionKind.Master, [.. MasterActions, "Label"]),
        new("m_itemgroup", "Item Group Master", "masterMgmt", 4, null, FunctionKind.Master, MasterActions),
        new("m_item", "Item Master", "masterMgmt", 5, null, FunctionKind.Master, MasterActions),
        new("m_partner", "Business Partner Master", "masterMgmt", 6, null, FunctionKind.Master, [.. MasterActions, "Label"]),
        new("m_device", "Device Master", "masterMgmt", 7, null, FunctionKind.Master, [.. MasterActions, "Ping"]),
        new("m_user", "User Master", "masterMgmt", 8, null, FunctionKind.Master, [.. MasterActions, "Label"]),
        new("m_reason", "Reason Master", "masterMgmt", 9, null, FunctionKind.Master, MasterActions),
        new("m_process", "Process Master", "masterMgmt", 10, "production", FunctionKind.Master, ["Open", "New", "Edit", "Copy", "Delete", "Label", "Export"]),
        new("m_location_3d", "Location 3D", "masterMgmt", 11, null, FunctionKind.Master, ["Open"]),
        new("dist_status", "Data Distribution", "masterMgmt", 12, null, FunctionKind.Admin, ["Open"]),
        new("m_permission", "User Permission", "otherMgmt", 1, null, FunctionKind.Special, ["Open", "New", "Edit", "Delete", "Save"]),
        new("settings", "Settings", "otherMgmt", 2, null, FunctionKind.Special, ["Open", "Save"]),
        new("lang_data", "Language Data", "otherMgmt", 3, null, FunctionKind.Transaction, ["Open", "Import", "Export"]),
        new("super_admin", "Super Admin", "otherMgmt", 4, null, FunctionKind.SuperAdmin, []),
        new("ht_receiving", "Receiving", "menu_wh", 1, "receiving", FunctionKind.Handheld, ["Open"]),
        new("ht_returns", "Returns", "menu_wh", 2, "returns", FunctionKind.Handheld, ["Open"]),
        new("ht_picking", "Picking", "menu_wh", 3, "picking", FunctionKind.Handheld, ["Open"]),
        new("ht_shipping", "Shipping", "menu_wh", 4, "shipping", FunctionKind.Handheld, ["Open"]),
        new("ht_inventory", "Stock Inquiry", "menu_wh", 5, null, FunctionKind.Handheld, ["Open"]),
        new("ht_transfer", "Location Transfer", "menu_wh", 6, null, FunctionKind.Handheld, ["Open"]),
        new("ht_prd_receipt", "Material Receipt", "ht_production", 1, "production", FunctionKind.Handheld, ["Open"]),
        new("ht_prd_input", "Material Input", "ht_production", 2, "production", FunctionKind.Handheld, ["Open"]),
        new("ht_prd_return", "Material Return", "ht_production", 3, "production", FunctionKind.Handheld, ["Open"]),
        new("ht_prd_output", "Production Completion", "ht_production", 4, "production", FunctionKind.Handheld, ["Open"]),
        new("ht_disposal", "Disposal", "menu_cm", 1, null, FunctionKind.Handheld, ["Open"]),
        new("ht_stocktaking", "Stocktaking", "menu_cm", 2, "stocktaking", FunctionKind.Handheld, ["Open"]),
    ];

    public static string PermissionCode(string functionCode, string actionType) => functionCode + "." + actionType;

    public static CatalogFunction? Find(string functionCode) =>
        Functions.FirstOrDefault(f => f.Code == functionCode);

    public static string? ModuleOf(string functionCode) => Find(functionCode)?.Module;

    public static string DeviceOf(string functionCode) =>
        functionCode.StartsWith("ht_", StringComparison.Ordinal) ? "HT" : "PC";

    public static bool DefaultAllowed(string roleCode, CatalogFunction f, string actionType) => roleCode switch
    {
        GeneralRole => f.Kind switch
        {
            FunctionKind.Handheld => true,
            FunctionKind.Admin => false,
            FunctionKind.Master or FunctionKind.Special => actionType == Open,
            FunctionKind.Transaction => !(f.Code == "prd_result" && actionType == "Reverse"),
            _ => false,
        },
        WorkerRole => f.Kind == FunctionKind.Handheld,
        _ => false,
    };
}
