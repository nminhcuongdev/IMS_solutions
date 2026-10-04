namespace Ims.Domain.Stocktaking;

public class StocktakingLabel
{
    public string SerialNo { get; set; } = null!;

    public long StocktakingSessionNo { get; set; }
    public string ItemCode { get; set; } = null!;
    public string LocationCode { get; set; } = null!;
    public string? LotNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public bool IsScanned { get; set; }

    public int Status { get; set; } = StocktakingLabelStatus.NotStarted;

    public DateTime? UpdatedAt { get; set; }

    public StocktakingSession? Session { get; set; }
    public Masters.Item? Item { get; set; }
    public Masters.Location? Location { get; set; }
}
