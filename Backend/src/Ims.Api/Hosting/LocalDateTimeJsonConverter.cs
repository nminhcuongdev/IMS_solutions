using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ims.Api.Hosting;

internal sealed class LocalDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? "";
        if (HasOffset(text) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
            return DateTime.SpecifyKind(withOffset.ToLocalTime().DateTime, DateTimeKind.Unspecified);
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var plain))
            return DateTime.SpecifyKind(plain, DateTimeKind.Unspecified);
        throw new JsonException($"Invalid date-time: {text}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(local, DateTimeKind.Local));
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        writer.WriteStringValue(local.ToString(Format, CultureInfo.InvariantCulture)
            + sign + abs.Hours.ToString("00", CultureInfo.InvariantCulture)
            + ":" + abs.Minutes.ToString("00", CultureInfo.InvariantCulture));
    }

    private static bool HasOffset(string text)
    {
        var t = text.IndexOf('T');
        if (t < 0) return false;
        var tail = text[(t + 1)..];
        return tail.EndsWith('Z') || tail.Contains('+') || tail.LastIndexOf('-') > 0;
    }
}
