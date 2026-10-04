using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ims.Domain.Masters;
using Ims.Domain.History;
using Ims.Domain.System;

namespace Ims.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task RunAsync(ImsDbContext db, CancellationToken ct = default)
    {
        await SeedStatus(db, ct);
        await SeedLanguage(db, ct);
        await SeedLanguageData(db, ct);
        await SeedExtraLabels(db, ct);
        await SeedDefaultValue(db, ct);
        await SeedFunctionLarge(db, ct);
        await SeedAppFunction(db, ct);
        await SeedPermission(db, ct);
        await SeedRole(db, ct);
        await SeedRolePermission(db, ct);
        await SeedMasters.RunAsync(db, ct);
        await SeedUsers(db, ct);
        await SeedHistory(db, ct);
        await SeedGeneral(db, ct);
        await SeedAlertConfig(db, ct);
        await SeedStockScan(db, ct);
        await db.SaveChangesAsync(ct);
        await SeedFunctionVisibility(db, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedUsers(ImsDbContext db, CancellationToken ct)
    {
        var have = await db.AppUsers.Select(u => u.UserCode).ToListAsync(ct);
        var hasher = new PasswordHasher<AppUser>();
        var admin = new AppUser
        {
            UserCode = "admin", UserName = "Administrator",
            RoleCode = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow,
        };
        admin.PasswordHash = hasher.HashPassword(admin, "password");
        var superAdmin = new AppUser
        {
            UserCode = "superadmin", UserName = "Super Administrator",
            RoleCode = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow,
        };
        superAdmin.PasswordHash = hasher.HashPassword(superAdmin, "superadmin");
        foreach (var u in new[] { admin, superAdmin })
            if (!have.Contains(u.UserCode)) db.AppUsers.Add(u);

        foreach (var r in (SeedMasters.Load()?.Users ?? new()).Where(u => !have.Contains(u.UserCode)))
        {
            var u = new AppUser
            {
                UserCode = r.UserCode, UserName = r.UserName,
                RoleCode = r.RoleCode, FactoryCode = null,
                DefaultLanguage = r.DefaultLanguage, IsActive = r.IsActive,
                Remark1 = r.Remark1, Remark2 = r.Remark2, Remark3 = r.Remark3,
                Remark4 = r.Remark4, Remark5 = r.Remark5,
                CreatedAt = DateTime.UtcNow,
            };
            u.PasswordHash = hasher.HashPassword(u, r.UserCode);
            db.AppUsers.Add(u);
        }
    }

    private static async Task SeedStatus(ImsDbContext db, CancellationToken ct)
    {
        var rows = new (int c, string? cat, string en, string ja, string vi)[]
        {
            (0,  null,         "Pending",          "保留",         "Chờ xử lý"),
            (1,  null,         "In Progress",      "処理中",       "Đang xử lý"),
            (2,  null,         "Completed",        "完了",         "Hoàn thành"),
            (3,  null,         "Canceled",         "キャンセル",   "Đã hủy"),
            (4,  null,         "Deleted",          "削除済み",     "Đã xóa"),
            (5,  "Inventory",  "On Hold",          "保留",         "Tạm giữ"),
            (6,  null,         "Error",            "エラー",       "Lỗi"),
            (7,  null,         "Returned",         "返品",         "Đã trả lại"),
            (8,  null,         "Waiting Approval", "承認待ち",     "Chờ duyệt"),
            (9,  null,         "Approved",         "承認済み",     "Đã duyệt"),
            (10, null,         "Rejected",         "却下",         "Từ chối"),
            (11, null,         "Skipped",          "スキップ",     "Bỏ qua"),
            (12, null,         "Timeout",          "タイムアウト", "Quá thời gian"),
            (13, null,         "Suspended",        "停止中",       "Tạm dừng"),
            (14, "Inventory",  "Normal",           "通常",         "Bình thường"),
            (15, "Master",     "Enable",           "有効",         "Kích hoạt"),
            (16, "Master",     "Disable",          "無効",         "Vô hiệu"),
            (17, "Expiration", "Expiring Soon",    "期限間近",     "Sắp hết hạn"),
            (18, "Expiration", "Expired",          "期限切れ",     "Đã hết hạn"),
            (19, "Print",      "Printed",          "印刷済み",     "Đã in"),
            (20, "Print",      "Reprinted",        "再印刷済み",   "Đã in lại"),
            (21, "Inventory",  "Finalized",        "棚卸確定",     "Đã chốt kiểm kê"),
            (22, "Inventory",  "Missing",          "行方不明",     "Thất lạc"),
            (23, "Inventory",  "Over",             "超過",         "Đếm thừa"),
        };
        var existing = await db.Statuses.Select(x => x.StatusCode).ToListAsync(ct);
        foreach (var r in rows.Where(r => !existing.Contains(r.c)))
        {
            db.Statuses.Add(new StatusMaster
            {
                StatusCode = r.c,
                Category = r.cat,
                StatusNameLang1 = r.en,
                StatusNameLang2 = r.ja,
                StatusNameLang3 = r.vi,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private static async Task SeedAlertConfig(ImsDbContext db, CancellationToken ct)
    {
        var rows = new (string code, string name, decimal? max, string note)[]
        {
            (AlertType.ExpiryWarning,  "Expiry Warning",  7m,
                "So NGAY con lai. Khac nguong 30 ngay cua bo loc 'Expiring Soon' o man Stock Inquiry: mot ben la de tra cuu, ben nay la de goi nguoi ta den lay hang."),
            (AlertType.LowStock,       "Low Stock",       null, "Nguong nam o item.reorder_level_1."),
            (AlertType.StockOverflow,  "Stock Overflow",  null, "Nguong nam o item.surplus_point."),
            (AlertType.HoldReview,     "Hold Review",     null, "Khong co nguong: hang dang bi giu thi dang bi giu."),
            (AlertType.ReceivingDelay, "Receiving Delay", 0m,   "So ngay qua han toi thieu."),
            (AlertType.PickingDelay,   "Picking Delay",   0m,   "So ngay qua han toi thieu."),
            (AlertType.ShippingDelay,  "Shipping Delay",  0m,   "So ngay qua han toi thieu."),
        };
        var existing = await db.AlertConfigs.Select(x => x.AlertTypeCode).ToListAsync(ct);
        foreach (var r in rows.Where(r => !existing.Contains(r.code)))
            db.AlertConfigs.Add(new AlertConfig
            {
                AlertTypeCode = r.code,
                AlertTypeName = r.name,
                MaxThreshold = r.max,
                IsActive = true,
                Remark1 = r.note,
                CreatedAt = DateTime.UtcNow,
            });
    }

    private static async Task SeedLanguage(ImsDbContext db, CancellationToken ct)
    {
        if (await db.Languages.AnyAsync(ct)) return;
        db.Languages.AddRange(
            new Language { LanguageCode = "en", LanguageName = "English",    IsActive = true, CreatedAt = DateTime.UtcNow },
            new Language { LanguageCode = "ja", LanguageName = "日本語",     IsActive = true, CreatedAt = DateTime.UtcNow },
            new Language { LanguageCode = "vi", LanguageName = "Tiếng Việt", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }

    private static async Task SeedHistory(ImsDbContext db, CancellationToken ct)
    {
        var raw = ReadSeedHistory();
        if (raw is null) return;

        if (!await db.PrintHistories.AnyAsync(ct) && raw.Print is not null)
        {
            foreach (var r in raw.Print)
            {
                var category = Strip(Str(r, 1), "printCat");
                db.PrintHistories.Add(new PrintHistory
                {
                    Category      = category,
                    CreatedAt     = ParseDateTime(Str(r, 0)) ?? DateTime.UtcNow,
                    DocNo         = Str(r, 2),
                    ItemCode      = Str(r, 3),
                    Qty           = Dec(r, 5),
                    PackageCount  = (int)(Dec(r, 6) ?? 1),
                    Spec          = Str(r, 7),
                    CreatedBy     = WorkerCode(Str(r, 8)),
                    Status        = Str(r, 9) == "statusReprinted" ? 20 : 19,
                    UomCode       = Str(r, 12),
                    ScheduledDate = ParseDate(Str(r, 13)),
                });
            }
        }

        if (!await db.RfidGateHistories.AnyAsync(ct) && raw.Rfid is not null)
        {
            foreach (var r in raw.Rfid)
            {
                db.RfidGateHistories.Add(new RfidGateHistory
                {
                    ItemCode      = Str(r, 0),
                    SerialNo      = Str(r, 2),
                    GateCode      = Str(r, 3) ?? "",
                    CreatedAt     = ParseDateTime(Str(r, 4)) ?? DateTime.UtcNow,
                    DirectionType = "In",
                    Status        = 2,
                });
            }
        }
    }

    private static string? WorkerCode(string? name)
        => name is null ? null
         : (SeedMasters.Load()?.Users ?? new()).FirstOrDefault(u => u.UserName == name)?.UserCode;

    private sealed class HistorySeed
    {
        public List<List<System.Text.Json.JsonElement>>? Print { get; set; }
        public List<List<System.Text.Json.JsonElement>>? Rfid { get; set; }
    }

    private static HistorySeed? ReadSeedHistory()
    {
        var asm = typeof(SeedData).Assembly;
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith("history.json", StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;
        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null) return null;
        return System.Text.Json.JsonSerializer.Deserialize<HistorySeed>(stream,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static string? Str(List<System.Text.Json.JsonElement> row, int i)
    {
        if (i >= row.Count) return null;
        var v = row[i];
        var s = v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : v.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static decimal? Dec(List<System.Text.Json.JsonElement> row, int i)
    {
        if (i >= row.Count) return null;
        var v = row[i];
        if (v.ValueKind == System.Text.Json.JsonValueKind.Number) return v.GetDecimal();
        return decimal.TryParse(v.GetString(), out var d) ? d : null;
    }

    private static string Strip(string? v, string prefix)
        => v is null ? "" : v.StartsWith(prefix, StringComparison.Ordinal) ? v[prefix.Length..] : v;

    private static DateTime? ParseDateTime(string? v)
        => DateTime.TryParse(v?.Replace('/', '-'), System.Globalization.CultureInfo.InvariantCulture,
                             System.Globalization.DateTimeStyles.None, out var d) ? d : null;

    private static DateOnly? ParseDate(string? v)
        => ParseDateTime(v) is { } d ? DateOnly.FromDateTime(d) : null;

    private static async Task SeedLanguageData(ImsDbContext db, CancellationToken ct)
    {
        if (await db.LanguageData.AnyAsync(ct)) return;

        var pc = ReadSeedJson("language_pc.json");
        var ht = ReadSeedJson("language_ht.json");
        if (pc is null && ht is null) return;

        var langs = new[] { "en", "ja", "vi" };
        var pcKeys = pc?.GetValueOrDefault("en")?.Keys.ToHashSet() ?? new HashSet<string>();
        var htKeys = ht?.GetValueOrDefault("en")?.Keys.ToHashSet() ?? new HashSet<string>();

        string? Text(Dictionary<string, Dictionary<string, string>>? src, string lang, string key)
            => src?.GetValueOrDefault(lang)?.GetValueOrDefault(key);

        void Add(string key, string lang, string? text, string? scope)
        {
            if (text is null) return;
            db.LanguageData.Add(new LanguageData
            {
                LabelKey = key, LanguageCode = lang, LabelText = text,
                DeviceScope = scope, CreatedAt = DateTime.UtcNow,
            });
        }

        foreach (var key in pcKeys.Union(htKeys))
        {
            var inPc = pcKeys.Contains(key);
            var inHt = htKeys.Contains(key);
            var identical = inPc && inHt
                && langs.All(l => Text(pc, l, key) == Text(ht, l, key));

            foreach (var lang in langs)
            {
                if (identical)             Add(key, lang, Text(pc, lang, key), null);
                else
                {
                    if (inPc) Add(key, lang, Text(pc, lang, key), "PC");
                    if (inHt) Add(key, lang, Text(ht, lang, key), "HT");
                }
            }
        }
    }

    private static async Task SeedExtraLabels(ImsDbContext db, CancellationToken ct)
    {
        var rows = new (string key, string en, string ja, string vi)[]
        {
            ("loading",       "Loading…",               "読み込み中…",          "Đang tải…"),
            ("pgTotal",       "Total:",                 "合計：",                "Tổng:"),
            ("signingIn",     "Signing in…",            "サインイン中…",        "Đang đăng nhập…"),
            ("errMenuLoad",   "Menu load failed",       "メニューの取得に失敗", "Không tải được menu"),
            ("thGroup",       "Group",                  "グループ",              "Nhóm"),
            ("thPacking",     "Packing",                "荷姿",                  "Quy cách đóng gói"),
            ("thParent",      "Parent",                 "親",                    "Cấp cha"),
            ("thType",        "Type",                   "種別",                  "Loại"),
            ("thZone",        "Zone",                   "ゾーン",                "Khu vực"),
            ("saVisibilityShow", "Show",                "表示",                  "Hiện"),
            ("saVisibilityHide", "Hide",                "非表示",                "Ẩn"),
            ("loginSubtitle",    "Inventory Management System",
                                                        "在庫管理システム",      "Hệ thống quản lý kho"),
            ("permSaved",        "Permissions saved.",  "権限を保存しました。",  "Đã lưu phân quyền."),
            ("thShortageName",   "Reason Name",         "理由名",                "Tên lý do"),
            ("searchAnyText",    "Search any text...",  "任意の文字で検索…",     "Tìm theo bất kỳ chữ nào…"),
            ("searchPrintHistory", "Doc No, item, printer, worker...",
                                                        "伝票No・品目・プリンタ・作業者…",
                                                        "Số chứng từ, hàng hóa, máy in, người thao tác…"),
            ("searchRfidHistory", "Item, EPC, gate...", "品目・EPC・ゲート…",     "Hàng hóa, EPC, cổng…"),
            ("saving",           "Saving…",             "保存中…",               "Đang lưu…"),
            ("comingSoon",       "Coming soon",         "準備中",                "Sắp có"),
            ("htReasonScopeByItemGroup", "By Item Group", "品目グループ別",        "Theo nhóm sản phẩm"),
            ("thExpectedQty",    "Expected",            "予定数量",              "Số dự kiến"),
            ("thReturnDate",     "Return Date",         "返品日",                "Ngày trả hàng"),
        };

        var have = await db.LanguageData
            .Where(d => d.DeviceScope == "PC")
            .Select(d => d.LabelKey + "|" + d.LanguageCode)
            .ToListAsync(ct);

        foreach (var r in rows)
            foreach (var (code, text) in new[] { ("en", r.en), ("ja", r.ja), ("vi", r.vi) })
                if (!have.Contains(r.key + "|" + code))
                    db.LanguageData.Add(new LanguageData
                    {
                        LabelKey = r.key, LanguageCode = code, LabelText = text,
                        DeviceScope = "PC", CreatedAt = DateTime.UtcNow,
                    });
    }

    private static Dictionary<string, Dictionary<string, string>>? ReadSeedJson(string fileName)
    {
        var asm = typeof(SeedData).Assembly;
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;
        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null) return null;
        return System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, Dictionary<string, string>>>(stream);
    }

    private static async Task SeedDefaultValue(ImsDbContext db, CancellationToken ct)
    {
        var have = await db.DefaultValues.Select(d => d.ValueType + "|" + d.ValueCode).ToListAsync(ct);
        var rows = new (string type, string code, string en, string ja, string vi, int order)[]
        {
            ("PackingType", "packingLoose", "Loose",  "バラ",   "Rời",        1),
            ("PackingType", "packingBox",   "Box",    "箱",     "Hộp",        2),
            ("PackingType", "packingCase",  "Case",   "ケース", "Thùng",      3),
            ("HoldName",    "holdNormal",   "Normal", "正常",   "Bình thường", 1),
            ("HoldName",    "holdOn",       "On Hold","保留中", "Đang giữ",    2),
        };
        foreach (var p in rows.Where(r => !have.Contains(r.type + "|" + r.code)))
        {
            db.DefaultValues.Add(new DefaultValue
            {
                ValueType = p.type,
                ValueCode = p.code,
                NameLang1 = p.en,
                NameLang2 = p.ja,
                NameLang3 = p.vi,
                DisplayOrder = p.order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private static async Task SeedGeneral(ImsDbContext db, CancellationToken ct)
    {
        var have = await db.GeneralSettings.Select(g => g.ConfigCode).ToListAsync(ct);
        var rows = new (string code, string? value, string cat)[]
        {
            ("default_factory",        "FAC-01",                          "Factory"),
            ("ht_label_mode_rcv",      "printNow",                        "HT"),
            ("ht_label_mode_shp",      "printNow",                        "HT"),
            ("ht_label_mode_rtn",      "printNow",                        "HT"),
            ("ht_reason_scope",        "byFunction",                      "HT"),
            ("ht_apk_version",         "1.0.0",                           "HT"),
            ("ht_apk_download_url",    null,                              "HT"),
            ("ht_printer_drivers",     "IND4-J,IND6-J,HND208,LT408",   "HT"),
            ("layout_path",            @"C:\ProgramData\LabelEngine\Layout_IMS\", "Layout"),
            ("layout_file_rcv",        "Receiving.mllayx",                "Layout"),
            ("layout_file_returns",    "Returns.mllayx",                  "Layout"),
            ("layout_file_shp",        "Shipping.mllayx",                 "Layout"),
            ("layout_file_m_location", "Location.mllayx",                 "Layout"),
            ("layout_file_m_user",     "User.mllayx",                     "Layout"),
            ("layout_file_m_partner",  "BusinessPartner.mllayx",          "Layout"),
            ("language_slot_1",           "en",     "Language"),
            ("language_slot_2",           "ja",     "Language"),
            ("language_slot_3",           "vi",     "Language"),
            ("language_slot_1_flag_icon", "us.png", "Language"),
            ("language_slot_2_flag_icon", "jp.png", "Language"),
            ("language_slot_3_flag_icon", "vn.png", "Language"),
        };
        foreach (var r in rows.Where(r => !have.Contains(r.code)))
            db.GeneralSettings.Add(new GeneralSetting
            {
                ConfigCode = r.code, ConfigValue = r.value, Category = r.cat,
                CreatedAt = DateTime.UtcNow,
            });
    }

    private static async Task SeedStockScan(ImsDbContext db, CancellationToken ct)
    {
        if (await db.StockScans.AnyAsync(ct)) return;
        var asm = typeof(SeedData).Assembly;
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith("stock_scan.json", StringComparison.OrdinalIgnoreCase));
        if (name is null) return;
        await using var stream = asm.GetManifestResourceStream(name);
        if (stream is null) return;
        var rows = await System.Text.Json.JsonSerializer.DeserializeAsync<List<Ims.Domain.Stocktaking.StockScan>>(
            stream, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
        if (rows is not null) db.StockScans.AddRange(rows);
    }

    private static async Task SeedFunctionVisibility(ImsDbContext db, CancellationToken ct)
    {
        if (await db.FunctionVisibilities.AnyAsync(ct)) return;
        var funcs = await db.AppFunctions.Where(f => !f.IsDeleted)
                            .Select(f => f.FunctionCode).ToListAsync(ct);
        foreach (var f in funcs)
            foreach (var device in new[] { "PC", "HT" })
                db.FunctionVisibilities.Add(new FunctionVisibility
                {
                    TargetType = device, TargetCode = f, IsVisible = true,
                });
    }

    private static async Task SeedFunctionLarge(ImsDbContext db, CancellationToken ct)
    {
        if (await db.FunctionLarges.AnyAsync(ct)) return;
        var rows = new (string code, string name, int order)[]
        {
            ("menu",             "Menu",             1),
            ("inventoryMgmt",    "Inventory",        2),
            ("alertCenter",      "Alert Center",     3),
            ("receivingMgmt",    "Receiving",        4),
            ("shippingMgmt",     "Shipping",         5),
            ("stocktakeMgmt",    "Stocktaking",      6),
            ("printHistory",     "Print History",    7),
            ("rfidHistory",      "RFID History",     8),
            ("masterMgmt",       "Master Data",      9),
            ("otherMgmt",        "Other",            10),
        };
        foreach (var r in rows)
        {
            db.FunctionLarges.Add(new FunctionLarge
            {
                LargeCode = r.code,
                LargeName = r.name,
                DisplayOrder = r.order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private static async Task SeedAppFunction(ImsDbContext db, CancellationToken ct)
    {
        if (await db.AppFunctions.AnyAsync(ct)) return;
        var rows = new (string code, string name, string large, int order)[]
        {
            ("dashboard",         "Menu",                       "menu",          1),
            ("m_factory",         "Factory Master",             "masterMgmt",    1),
            ("m_zone",            "Zone Master",                "masterMgmt",    2),
            ("m_location",        "Location Master",            "masterMgmt",    3),
            ("m_itemgroup",       "Item Group Master",          "masterMgmt",    4),
            ("m_item",            "Item Master",                "masterMgmt",    5),
            ("m_partner",         "Business Partner Master",    "masterMgmt",    6),
            ("m_device",          "Device Master",              "masterMgmt",    7),
            ("m_user",            "User Master",                "masterMgmt",    8),
            ("m_reason",          "Reason Master",              "masterMgmt",    9),
            ("m_location_3d",     "Location 3D",                "masterMgmt",    10),
            ("inv",              "Stock Inquiry",              "inventoryMgmt", 1),
            ("inv_history",      "Work History",               "inventoryMgmt", 2),
            ("inv_hold",         "Hold Item",                  "inventoryMgmt", 3),
            ("rcv_import",        "Import Receiving Schedule",  "receivingMgmt", 1),
            ("rcv_result",        "Receiving/Returns Result",   "receivingMgmt", 3),
            ("returns",           "Returns",                    "receivingMgmt", 2),
            ("pick_import",       "Import Picking Schedule",    "shippingMgmt",  1),
            ("pick_result",       "Picking Result",             "shippingMgmt",  2),
            ("shp_import",        "Import Shipping Schedule",   "shippingMgmt",  3),
            ("shp_result",        "Shipping Result",            "shippingMgmt",  4),
            ("st_label",          "Item Stocktaking",           "stocktakeMgmt", 1),
            ("st_ref",            "Stocktaking History",        "stocktakeMgmt", 2),
            ("st_rfid_demo",      "RFID Stock Count",           "stocktakeMgmt", 3),
            ("alert_center",      "Alert Center",               "alertCenter",   1),
            ("print_history",     "Print History",              "printHistory",  1),
            ("rfid_gate_history", "RFID Gate History",          "rfidHistory",   1),
            ("m_permission",      "User Permission",            "otherMgmt",     1),
            ("settings",          "Settings",                   "otherMgmt",     2),
            ("lang_data",         "Language Data",              "otherMgmt",     3),
            ("super_admin",       "Super Admin",                "otherMgmt",     4),
        };
        foreach (var r in rows)
        {
            db.AppFunctions.Add(new AppFunction
            {
                FunctionCode = r.code,
                FunctionName = r.name,
                LargeCode = r.large,
                DisplayOrder = r.order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private static async Task SeedPermission(ImsDbContext db, CancellationToken ct)
    {
        if (await db.Permissions.AnyAsync(ct)) return;
        var masterScreens = new[] { "m_factory","m_zone","m_location","m_itemgroup","m_item","m_partner","m_user","m_reason" };
        var masterActions = new[] { "view","new","edit","delete","import","export" };
        foreach (var f in masterScreens)
            foreach (var a in masterActions)
                db.Permissions.Add(new Permission
                {
                    PermissionCode = $"{f}.{a}",
                    FunctionCode = f,
                    Action = a,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
        foreach (var f in new[] { "m_location", "m_user", "m_partner" })
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"{f}.label",
                FunctionCode = f,
                Action = "label",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

        var otherScreens = new[] { "dashboard","m_permission","settings","lang_data","print_history","rfid_gate_history","super_admin","rcv_result","shp_result","pick_result","m_location_3d","alert_center","m_device" };
        foreach (var f in otherScreens)
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"{f}.view",
                FunctionCode = f,
                Action = "view",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

        foreach (var f in new[] { "rcv_import", "returns", "shp_import" })
        foreach (var a in new[] { "view", "new", "status", "shortage", "import", "export" })
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"{f}.{a}",
                FunctionCode = f,
                Action = a,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        foreach (var a in new[] { "view", "new", "status", "import", "export" })
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"pick_import.{a}",
                FunctionCode = "pick_import",
                Action = a,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        foreach (var (f, actions) in new[]
        {
            ("inv", new[] { "view", "adjust", "disposal", "export" }),
            ("inv_history", new[] { "view", "restore", "export" }),
            ("inv_hold", new[] { "view", "hold", "release", "export" }),
        })
        foreach (var a in actions)
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"{f}.{a}",
                FunctionCode = f,
                Action = a,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

        foreach (var (f, actions) in new[]
        {
            ("st_label", new[] { "view", "start", "complete", "finalize", "export" }),
            ("st_ref", new[] { "view", "export" }),
            ("st_rfid_demo", new[] { "view", "reset" }),
        })
        foreach (var a in actions)
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"{f}.{a}",
                FunctionCode = f,
                Action = a,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

        foreach (var f in new[] { "rcv_result", "shp_result", "pick_result" })
            db.Permissions.Add(new Permission
            {
                PermissionCode = $"{f}.export",
                FunctionCode = f,
                Action = "export",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
    }

    private static async Task SeedRole(ImsDbContext db, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(ct)) return;
        db.Roles.AddRange(
            new Role { RoleCode = "Admin",   RoleName = "Administrator", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Role { RoleCode = "General", RoleName = "General User",  IsActive = true, CreatedAt = DateTime.UtcNow },
            new Role { RoleCode = "Worker",  RoleName = "Worker",        IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }

    private static async Task SeedRolePermission(ImsDbContext db, CancellationToken ct)
    {
        if (await db.RolePermissions.AnyAsync(ct)) return;
        var permCodes = db.ChangeTracker.Entries<Permission>()
                          .Select(e => (e.Entity.PermissionCode, e.Entity.Action))
                          .ToList();
        foreach (var (code, action) in permCodes)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleCode = "Admin", PermissionCode = code, IsAllowed = true,
                CreatedAt = DateTime.UtcNow,
            });
            db.RolePermissions.Add(new RolePermission
            {
                RoleCode = "General", PermissionCode = code,
                IsAllowed = action is "view" or "export" or "import" or "new" or "edit" or "label",
                CreatedAt = DateTime.UtcNow,
            });
            db.RolePermissions.Add(new RolePermission
            {
                RoleCode = "Worker", PermissionCode = code,
                IsAllowed = action is "view" or "label",
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

}
