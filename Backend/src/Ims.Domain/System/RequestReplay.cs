namespace Ims.Domain.System;

public class RequestReplay
{
    public string RequestId { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public int StatusCode { get; set; }
    public string? Location { get; set; }
    public string ResponseJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
