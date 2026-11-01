namespace Ims.Api.Features.Distribution;

internal interface IDistStorage
{
    Task SaveAsync(string fileName, byte[] bytes, CancellationToken ct);

    Stream? OpenRead(string fileName);

    void Delete(string fileName);
}
