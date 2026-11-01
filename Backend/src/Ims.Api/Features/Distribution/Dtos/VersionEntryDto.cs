using System.Text.Json.Serialization;

namespace Ims.Api.Features.Distribution;

public sealed record VersionEntryDto(
    [property: JsonPropertyName("packageName")] string PackageName,
    [property: JsonPropertyName("versionCode")] int VersionCode,
    [property: JsonPropertyName("versionName")] string VersionName,
    [property: JsonPropertyName("updatedAt")] string UpdatedAt,
    [property: JsonPropertyName("apkFile")] string ApkFile,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("minVersionCode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MinVersionCode,
    [property: JsonPropertyName("force"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Force,
    [property: JsonPropertyName("notes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, string>? Notes);
