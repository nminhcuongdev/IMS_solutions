namespace Ims.Api.Contracts;

public interface IClock
{
    DateTime Now { get; }
}
