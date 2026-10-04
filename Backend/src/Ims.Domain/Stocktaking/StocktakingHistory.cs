namespace Ims.Domain.Stocktaking;

public class StocktakingHistory
{
    public string ItemCode { get; set; } = null!;

    public string LotNo { get; set; } = "";

    public string StocktakingName { get; set; } = null!;
    public DateTime StocktakingDate { get; set; }

    public decimal PlannedQty { get; set; }

    public decimal ResultQty { get; set; }

    public decimal MissingQty { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Masters.Item? Item { get; set; }
}
