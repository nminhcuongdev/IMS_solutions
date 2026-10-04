namespace Ims.Domain.System;

public class RevokedToken
{
    public string Jti { get; set; } = null!;
    public string UserCode { get; set; } = null!;
    public DateTime RevokedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
