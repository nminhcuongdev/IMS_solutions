using System.Text;
using Ims.Api.Contracts;

namespace Ims.Api.Tests.Infrastructure;

internal static class CsvReader
{
    public static async Task<string> ReadAsync(ServiceResult result)
    {
        byte[] bytes;

        if (result.File is { } file)
        {
            bytes = file.Contents;
        }
        else
        {
            var stream = result.Stream ?? throw new InvalidOperationException($"Result with status {result.StatusCode} carries no file.");
            using var ms = new MemoryStream();
            await stream.Write(ms);
            bytes = ms.ToArray();
        }

        var text = Encoding.UTF8.GetString(bytes);
        return text.TrimStart('\uFEFF');
    }

    public static string? FileName(ServiceResult result) =>
        result.File?.FileName ?? result.Stream?.FileName;
}
