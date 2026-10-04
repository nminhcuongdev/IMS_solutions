namespace Ims.Domain.Shipping;

public class ShippingTemp
{
    public long No { get; set; }

    public string? SoNo { get; set; }
    public string? SoBranchNo { get; set; }
    public string? PartnerCode { get; set; }
    public string? ItemCode { get; set; }

    public decimal? Qty { get; set; }

    public string? FactoryCode { get; set; }
    public DateOnly? ScheduledDate { get; set; }

    public string? Msg { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
