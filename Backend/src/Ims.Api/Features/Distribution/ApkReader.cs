using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Ims.Api.Features.Distribution;

public static class ApkReader
{
    private const ushort ChunkStringPool = 0x0001;
    private const ushort ChunkXml = 0x0003;
    private const ushort ChunkResourceMap = 0x0180;
    private const ushort ChunkStartElement = 0x0102;
    private const uint NoIndex = 0xFFFFFFFF;
    private const uint AttrVersionCode = 0x0101021b;
    private const uint AttrVersionName = 0x0101021c;
    private const byte TypeString = 0x03;
    private const byte TypeIntDec = 0x10;
    private const byte TypeIntHex = 0x11;
    private static readonly uint[] SigningSchemes = [0x1b93ad61, 0xf05368c0, 0x7109871a];
    private static readonly byte[] SigningMagic = "APK Sig Block 42"u8.ToArray();

    public static ApkReadResult Read(byte[] apk)
    {
        byte[]? manifestBytes;
        try
        {
            using var zip = new ZipArchive(new MemoryStream(apk, false), ZipArchiveMode.Read);
            var entry = zip.GetEntry("AndroidManifest.xml");
            if (entry is null) return new("manifest", null, 0, null, null);
            using var s = entry.Open();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            manifestBytes = ms.ToArray();
        }
        catch (InvalidDataException) { return new("zip", null, 0, null, null); }

        var manifest = ReadManifest(manifestBytes);
        if (manifest is null) return new("manifest", null, 0, null, null);
        var cert = ReadSignerCertificate(apk);
        var fingerprint = cert is null ? null : FormatFingerprint(SHA256.HashData(cert));
        return new(fingerprint is null ? "signature" : null,
            manifest.PackageName, manifest.VersionCode, manifest.VersionName, fingerprint);
    }

    public static string FormatFingerprint(byte[] hash) => Convert.ToHexString(hash)
        .Chunk(2).Select(c => new string(c)).Aggregate((a, b) => a + ":" + b);

    public static ManifestFacts? ReadManifest(byte[] axml)
    {
        try { return ParseManifest(axml); }
        catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException
                                       or ArgumentException or DecoderFallbackException) { return null; }
    }

    private static ManifestFacts? ParseManifest(byte[] x)
    {
        if (x.Length < 8 || U16(x, 0) != ChunkXml) return null;
        var end = Math.Min(x.Length, (int)U32(x, 4));
        var pos = (int)U16(x, 2);
        string[] strings = [];
        uint[] resourceIds = [];
        while (pos + 8 <= end)
        {
            var type = U16(x, pos);
            var headerSize = U16(x, pos + 2);
            var size = (int)U32(x, pos + 4);
            if (size < 8 || pos + size > end) return null;
            if (type == ChunkStringPool) strings = ReadStringPool(x, pos);
            else if (type == ChunkResourceMap)
                resourceIds = Enumerable.Range(0, (size - headerSize) / 4)
                    .Select(i => U32(x, pos + headerSize + i * 4)).ToArray();
            else if (type == ChunkStartElement)
            {
                var ext = pos + headerSize;
                if (Str(strings, U32(x, ext + 4)) == "manifest")
                    return ReadManifestAttributes(x, ext, strings, resourceIds);
            }
            pos += size;
        }
        return null;
    }

    private static ManifestFacts? ReadManifestAttributes(byte[] x, int ext, string[] strings, uint[] resourceIds)
    {
        var start = ext + U16(x, ext + 8);
        var attrSize = U16(x, ext + 10);
        var count = U16(x, ext + 12);
        string? package = null, versionName = null;
        int? versionCode = null;
        for (var i = 0; i < count; i++)
        {
            var a = start + i * attrSize;
            var nameIndex = U32(x, a + 4);
            var raw = U32(x, a + 8);
            var dataType = x[a + 15];
            var data = U32(x, a + 16);
            var resId = nameIndex < resourceIds.Length ? resourceIds[nameIndex] : 0;
            var name = Str(strings, nameIndex);
            string? text = raw != NoIndex ? Str(strings, raw) : dataType == TypeString ? Str(strings, data) : null;
            if (resId == AttrVersionCode || resId == 0 && name == "versionCode")
                versionCode = dataType is TypeIntDec or TypeIntHex ? (int)data
                    : int.TryParse(text, out var v) ? v : null;
            else if (resId == AttrVersionName || resId == 0 && name == "versionName")
                versionName = text ?? (dataType is TypeIntDec ? data.ToString() : null);
            else if (name == "package") package = text;
        }
        return package is null || versionCode is null
            ? null
            : new ManifestFacts(package, versionCode.Value, versionName ?? versionCode.Value.ToString());
    }

    private static string[] ReadStringPool(byte[] x, int chunk)
    {
        var count = (int)U32(x, chunk + 8);
        var utf8 = (U32(x, chunk + 16) & (1 << 8)) != 0;
        var stringsStart = chunk + (int)U32(x, chunk + 20);
        var offsetsStart = chunk + U16(x, chunk + 2);
        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            var p = stringsStart + (int)U32(x, offsetsStart + i * 4);
            result[i] = utf8 ? ReadUtf8(x, p) : ReadUtf16(x, p);
        }
        return result;
    }

    private static string ReadUtf16(byte[] x, int p)
    {
        int len = U16(x, p);
        p += 2;
        if ((len & 0x8000) != 0) { len = ((len & 0x7FFF) << 16) | U16(x, p); p += 2; }
        return Encoding.Unicode.GetString(x, p, len * 2);
    }

    private static string ReadUtf8(byte[] x, int p)
    {
        p += (x[p] & 0x80) != 0 ? 2 : 1;
        int len = x[p];
        if ((len & 0x80) != 0) { len = ((len & 0x7F) << 8) | x[p + 1]; p += 2; } else p += 1;
        return Encoding.UTF8.GetString(x, p, len);
    }

    private static byte[]? ReadSignerCertificate(byte[] apk)
    {
        try
        {
            var eocd = FindEndOfCentralDirectory(apk);
            if (eocd < 0) return null;
            var cd = (long)U32(apk, eocd + 16);
            if (cd < 32 || cd > apk.Length) return null;
            if (!apk.AsSpan((int)cd - 16, 16).SequenceEqual(SigningMagic)) return null;
            var blockSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(apk.AsSpan((int)cd - 24));
            var blockStart = cd - blockSize - 8;
            if (blockStart < 0) return null;
            var pos = blockStart + 8;
            var pairsEnd = cd - 24;
            var values = new Dictionary<uint, (int Offset, int Length)>();
            while (pos + 12 <= pairsEnd)
            {
                var len = (long)BinaryPrimitives.ReadUInt64LittleEndian(apk.AsSpan((int)pos));
                if (len < 4 || pos + 8 + len > pairsEnd) return null;
                values[U32(apk, (int)pos + 8)] = ((int)pos + 12, (int)len - 4);
                pos += 8 + len;
            }
            foreach (var id in SigningSchemes)
                if (values.TryGetValue(id, out var v)) return FirstCertificate(apk, v.Offset, v.Length);
            return null;
        }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    private static byte[]? FirstCertificate(byte[] x, int offset, int length)
    {
        var signers = Prefixed(x, offset, offset + length);
        var signer = Prefixed(x, signers.Start, signers.End);
        var signedData = Prefixed(x, signer.Start, signer.End);
        var digests = Prefixed(x, signedData.Start, signedData.End);
        var certificates = Prefixed(x, digests.End, signedData.End);
        var cert = Prefixed(x, certificates.Start, certificates.End);
        return cert.End > cert.Start ? x[cert.Start..cert.End] : null;
    }

    private static (int Start, int End) Prefixed(byte[] x, int pos, int limit)
    {
        var len = (int)U32(x, pos);
        var start = pos + 4;
        if (len < 0 || start + len > limit) throw new ArgumentOutOfRangeException(nameof(pos));
        return (start, start + len);
    }

    private static int FindEndOfCentralDirectory(byte[] x)
    {
        var min = Math.Max(0, x.Length - 22 - 0xFFFF);
        for (var i = x.Length - 22; i >= min; i--)
            if (U32(x, i) == 0x06054b50 && i + 22 + U16(x, i + 20) == x.Length) return i;
        return -1;
    }

    private static string? Str(string[] strings, uint index) => index < strings.Length ? strings[index] : null;
    private static ushort U16(byte[] x, int p) => BinaryPrimitives.ReadUInt16LittleEndian(x.AsSpan(p, 2));
    private static uint U32(byte[] x, int p) => BinaryPrimitives.ReadUInt32LittleEndian(x.AsSpan(p, 4));
}
