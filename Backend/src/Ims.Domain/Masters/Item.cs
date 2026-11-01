using Ims.Domain.Common;

namespace Ims.Domain.Masters;

public class Item : SoftDeletableEntity
{
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string? ItemShortName { get; set; }
    public string ItemGroupCode { get; set; } = null!;
    public string? UomCode { get; set; }
    public string? PackingTypeCode { get; set; }
    public decimal PackageQty { get; set; } = 1;
    public bool IsLotManaged { get; set; }
    public bool HtScanRequired { get; set; } = true;
    public bool IsSerialManaged { get; set; }
    public string? Barcode { get; set; }
    public int? ExpirationPeriod { get; set; }
    public decimal? SurplusPoint { get; set; }
    public decimal? ReorderLevel1 { get; set; }
    public decimal? ReorderLevel2 { get; set; }
    public string? DefaultLocationCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remark1 { get; set; }
    public string? Remark2 { get; set; }
    public string? Remark3 { get; set; }
    public string? Remark4 { get; set; }
    public string? Remark5 { get; set; }

    public ItemGroup? ItemGroup { get; set; }
    public Location? DefaultLocation { get; set; }
}
