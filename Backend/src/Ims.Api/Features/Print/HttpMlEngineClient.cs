using System.Xml.Linq;

namespace Ims.Api.Features.Print;

public sealed class HttpMlEngineClient(HttpClient http) : IMlEngineClient
{
    public async Task<string> PreviewAsync(
        string layoutFile, string setting, IReadOnlyDictionary<string, string> fields, CancellationToken ct)
    {
        var instanceId = await CreateInstanceAsync(ct);
        try
        {
            await PropertyAccessAsync(instanceId, layoutFile, setting, fields, 1, ct);

            var preview = await CallAsync(
                "Preview",
                new Dictionary<string, string?>
                {
                    ["instanceId"] = instanceId,
                    ["outputMode"] = "0",
                    ["printAreaBorder"] = "False",
                    ["zoom"] = "500",
                },
                "Preview", ct);

            var image = Tag(preview, "image");
            if (string.IsNullOrEmpty(image)) throw new MlEngineException("Preview", "no <image> in response");
            return image;
        }
        finally
        {
            await DisposeAsync(instanceId, ct);
        }
    }

    public async Task PrintAsync(
        string layoutFile, string setting, IReadOnlyDictionary<string, string> fields, int copies, CancellationToken ct)
    {
        var instanceId = await CreateInstanceAsync(ct);
        try
        {
            await PropertyAccessAsync(instanceId, layoutFile, setting, fields, copies, ct);
            var id = new Dictionary<string, string?> { ["instanceId"] = instanceId };

            await CallAsync("OpenPort", id, "OpenPort", ct);
            try
            {
                await CallAsync("Output",
                    new Dictionary<string, string?> { ["instanceId"] = instanceId, ["outputMode"] = "0" }, "Output", ct);
            }
            finally
            {
                try { await CallAsync("ClosePort", id, null, CancellationToken.None); } catch { }
            }
        }
        finally
        {
            await DisposeAsync(instanceId, ct);
        }
    }

    private Task PropertyAccessAsync(
        string instanceId, string layoutFile, string setting,
        IReadOnlyDictionary<string, string> fields, int copies, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["instanceId"] = instanceId,
            ["setting"] = setting,
            ["protocol"] = "1",
            ["layoutFile"] = layoutFile,
        };
        foreach (var (k, v) in fields) query[$"prnData.{k}"] = v;
        query["prnData.Quantiy"] = copies.ToString();
        return CallAsync("PropertyAccess", query, "PropertyAccess", ct);
    }

    private async Task DisposeAsync(string instanceId, CancellationToken ct)
    {
        try { await CallAsync("DisposeInstance", new Dictionary<string, string?> { ["instanceId"] = instanceId }, null, ct); }
        catch { }
    }

    private async Task<string> CreateInstanceAsync(CancellationToken ct)
    {
        var doc = await CallAsync(
            "CreateInstance",
            new Dictionary<string, string?> { ["app"] = "IMS", ["disposeTime"] = "5", ["apiVersion"] = "1.0" },
            "CreateInstance", ct);
        return Tag(doc, "instanceId") ?? throw new MlEngineException("CreateInstance", "no <instanceId> in response");
    }

    private async Task<XDocument> CallAsync(
        string function, IReadOnlyDictionary<string, string?> query, string? stepForError, CancellationToken ct)
    {
        var qs = string.Join("&", query
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
        var res = await http.GetAsync($"{function}?{qs}", ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(text);

        if (stepForError is not null && Tag(doc, "result") != "OK")
        {
            var cc = Tag(doc, "componentCode");
            var msg = Tag(doc, "message");
            var detail = string.Join(" / ", new[] { cc is null ? null : $"componentCode={cc}", msg }
                .Where(s => s is not null));
            throw new MlEngineException(stepForError, detail.Length == 0 ? null : detail,
                int.TryParse(cc, out var code) ? code : null);
        }
        return doc;
    }

    private static string? Tag(XDocument doc, string name) => doc.Root?.Element(name)?.Value;
}
