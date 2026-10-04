using System.Globalization;
using System.Text;

namespace Ims.Api.Contracts;

public static class FieldHelpers
{
    public static readonly char[] BadCodeChars = { '/', '?', '&', '=', '#', '%' };

    public static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();


    public const int MaxCodeLength = 20;

    public static ServiceResult? ValidateCode(string? code, int maxLength = MaxCodeLength)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val001()));
        if (code.AsSpan().IndexOfAny(BadCodeChars) >= 0)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005()));
        if (code.Trim().Length > maxLength)
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val005TooLong(code.Trim(), maxLength)));
        return null;
    }

    public static ServiceResult? ValidateName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? Outcome.BadRequest(new ApiError(ErrorCatalog.Val002()))
            : null;

    public static ServiceResult? ValidateIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var parts = ip.Trim().Split('.');
        if (parts.Length != 4 || parts.Any(p => p.Length == 0 || p.Length > 3
                || !p.All(char.IsAsciiDigit) || !byte.TryParse(p, out _)))
            return Outcome.BadRequest(new ApiError(ErrorCatalog.Val004()));
        return null;
    }

    public static string Csv(string? v)
    {
        if (v is null) return "";
        return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + v.Replace("\"", "\"\"") + "\""
            : v;
    }

    public static string Num(decimal? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "";
    public static string Num(decimal v)  => v.ToString(CultureInfo.InvariantCulture);
    public static string Num(int? v)     => v?.ToString(CultureInfo.InvariantCulture) ?? "";

    public static ServiceResult CsvFile(StringBuilder sb, string fileName)
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Outcome.File(bytes, "text/csv; charset=utf-8", fileName);
    }

    public static ServiceResult CsvStream<T>(
        string fileName, string header, IAsyncEnumerable<T> rows, Func<T, string> toLine)
        => Outcome.Stream(async stream =>
        {
            await using var w = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                                                 bufferSize: 64 * 1024, leaveOpen: true);
            await w.WriteLineAsync(header);
            var since = 0;
            await foreach (var r in rows)
            {
                await w.WriteLineAsync(toLine(r));
                if (++since >= 5000) { await w.FlushAsync(); since = 0; }
            }
            await w.FlushAsync();
        }, "text/csv; charset=utf-8", fileName);

    public static int NormalizePageSize(int? pageSize)
        => pageSize is 20 or 50 or 100 or 200 ? pageSize.Value : 50;

    public static int NormalizePage(int? page) => Math.Max(1, page ?? 1);
}
