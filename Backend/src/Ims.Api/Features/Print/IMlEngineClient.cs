namespace Ims.Api.Features.Print;

public interface IMlEngineClient
{
    Task<string> PreviewAsync(
        string layoutFile, string setting, IReadOnlyDictionary<string, string> fields, CancellationToken ct);

    Task PrintAsync(
        string layoutFile, string setting, IReadOnlyDictionary<string, string> fields, int copies, CancellationToken ct);
}
