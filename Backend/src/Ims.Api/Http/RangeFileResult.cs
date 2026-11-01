using System.Globalization;

namespace Ims.Api.Http;

public sealed class RangeFileResult(
    Func<Stream> open, long length, string contentType, DateTime? lastModified, Func<Task>? onCompleted) : IResult
{
    public async Task ExecuteAsync(HttpContext http)
    {
        var res = http.Response;
        res.Headers.AcceptRanges = "bytes";
        res.ContentType = contentType;
        if (lastModified is { } lm) res.Headers.LastModified = new DateTimeOffset(lm).ToString("R", CultureInfo.InvariantCulture);
        long from = 0, to = length - 1;
        var range = http.Request.Headers.Range.ToString();
        if (range.Length > 0)
        {
            if (!TryParseRange(range, length, out from, out to))
            {
                res.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                res.Headers.ContentRange = $"bytes */{length}";
                return;
            }
            res.StatusCode = StatusCodes.Status206PartialContent;
            res.Headers.ContentRange = $"bytes {from}-{to}/{length}";
        }
        else res.StatusCode = StatusCodes.Status200OK;
        res.ContentLength = length == 0 ? 0 : to - from + 1;
        if (HttpMethods.IsHead(http.Request.Method) || length == 0) return;

        await using var stream = open();
        stream.Seek(from, SeekOrigin.Begin);
        var buffer = new byte[81920];
        var left = to - from + 1;
        while (left > 0)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, left)), http.RequestAborted);
            if (n == 0) break;
            await res.Body.WriteAsync(buffer.AsMemory(0, n), http.RequestAborted);
            left -= n;
        }
        if (left == 0 && to == length - 1 && onCompleted is not null)
        {
            await res.Body.FlushAsync(http.RequestAborted);
            await onCompleted();
        }
    }

    public static bool TryParseRange(string header, long length, out long from, out long to)
    {
        from = 0;
        to = length - 1;
        if (!header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) || header.Contains(',')) return false;
        var spec = header[6..].Trim();
        var dash = spec.IndexOf('-');
        if (dash < 0) return false;
        var a = spec[..dash].Trim();
        var b = spec[(dash + 1)..].Trim();
        if (a.Length == 0)
        {
            if (!long.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out var suffix) || suffix == 0) return false;
            from = Math.Max(0, length - suffix);
            return length > 0;
        }
        if (!long.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out from) || from >= length) return false;
        if (b.Length > 0)
        {
            if (!long.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out var end) || end < from) return false;
            to = Math.Min(end, length - 1);
        }
        return true;
    }
}
