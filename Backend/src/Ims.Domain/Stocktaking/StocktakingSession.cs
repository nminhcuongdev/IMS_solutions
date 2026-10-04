namespace Ims.Domain.Stocktaking;

public class StocktakingSession
{
    public long No { get; set; }

    public string SessionName { get; set; } = null!;

    public int Status { get; set; } = StocktakingSessionStatus.InProgress;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool AcceptsResults => Status == StocktakingSessionStatus.InProgress;
}
