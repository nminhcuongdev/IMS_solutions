namespace Ims.Api.Features.Print;

public sealed class MlEngineException(string step, string? detail, int? componentCode = null)
    : Exception($"{step}{(detail is null ? "" : $": {detail}")}")
{
    public int? ComponentCode { get; } = componentCode;
}
