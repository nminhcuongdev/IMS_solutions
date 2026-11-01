using Ims.Domain.Masters;

namespace Ims.Api.Features.Alerts;

internal sealed class ItemTotal
{
    public Item Item { get; init; } = null!;
    public decimal Total { get; init; }
}
