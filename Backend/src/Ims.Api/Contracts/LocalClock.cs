namespace Ims.Api.Contracts;

internal sealed class LocalClock : IClock
{
    public DateTime Now => DateTime.Now;
}
