using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Ims.Api.Tests.Guards;

public class SourceGuardTests
{
    private static string BackendRoot([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", ".."));

    private static IEnumerable<string> Sources(string root, params string[] patterns) =>
        patterns.SelectMany(p => Directory.EnumerateFiles(root, p, SearchOption.AllDirectories))
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                     && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

    [Fact]
    public void No_utc_clock_is_used_anywhere_in_the_server()
    {
        var src = Path.Combine(BackendRoot(), "src");
        var hits = Sources(src, "*.cs")
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => Regex.IsMatch(x.line, @"\bUtcNow\b|GETUTCDATE|SYSUTCDATETIME", RegexOptions.IgnoreCase))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.True(hits.Count == 0, "UTC clock used at: " + string.Join(", ", hits));
    }

    private static readonly Regex Vietnamese =
        new("[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]", RegexOptions.IgnoreCase);

    [Fact]
    public void No_vietnamese_text_outside_the_language_files()
    {
        var root = BackendRoot();
        var files = Sources(Path.Combine(root, "src"), "*.cs")
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar)
                     && !f.EndsWith(Path.DirectorySeparatorChar + "SeedData.cs", StringComparison.Ordinal));
        var hits = files
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, i, line)))
            .Where(x => Vietnamese.IsMatch(x.line))
            .Select(x => $"{Path.GetFileName(x.f)}:{x.i + 1}")
            .ToList();
        Assert.True(hits.Count == 0, "Vietnamese text in source: " + string.Join(", ", hits));
    }
}
