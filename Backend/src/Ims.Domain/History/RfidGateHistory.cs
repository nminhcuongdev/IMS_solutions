namespace Ims.Domain.History;

public class RfidGateHistory
{
    public long No { get; set; }
    public DateTime CreatedAt { get; set; }
    public string GateCode { get; set; } = null!;
    public string? ItemCode { get; set; }
    public string? LotNo { get; set; }
    public string? SerialNo { get; set; }
    public string? DirectionType { get; set; }
    public int Status { get; set; }
}
