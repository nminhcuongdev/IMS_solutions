using System.IO.Compression;
using System.Text;

namespace Ims.Api.Tests.Distribution;

internal static class ApkBuilder
{
    public const uint V2 = 0x7109871a;
    public const uint V3 = 0xf05368c0;

    public static readonly byte[] ReleaseCert = Encoding.ASCII.GetBytes("release-certificate-der");
    public static readonly byte[] DebugCert = Encoding.ASCII.GetBytes("debug-certificate-der");

    public static byte[] Build(string package = "com.imssolutions.ht", int versionCode = 10500,
        string versionName = "1.5.0", byte[]? cert = null, uint? scheme = V2, bool utf8 = false,
        bool withManifest = true, int padding = 0)
    {
        var zip = new MemoryStream();
        using (var a = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (withManifest)
            {
                using var s = a.CreateEntry("AndroidManifest.xml").Open();
                s.Write(Manifest(package, versionCode, versionName, utf8));
            }
            using (var s = a.CreateEntry("classes.dex", CompressionLevel.NoCompression).Open())
                s.Write(new byte[16 + padding]);
        }
        var bytes = zip.ToArray();
        return scheme is { } id ? Splice(bytes, SigningBlock(id, cert ?? ReleaseCert)) : bytes;
    }

    public static byte[] Manifest(string package, int versionCode, string versionName, bool utf8)
    {
        var strings = new[] { "versionCode", "versionName", "package", "manifest", package, versionName,
            "http://schemas.android.com/apk/res/android" };
        var pool = StringPool(strings, utf8);
        var map = new MemoryStream();
        U16(map, 0x0180); U16(map, 8); U32(map, 16); U32(map, 0x0101021b); U32(map, 0x0101021c);

        var el = new MemoryStream();
        U16(el, 0x0102); U16(el, 16); U32(el, 16 + 20 + 60); U32(el, 1); U32(el, 0xFFFFFFFF);
        U32(el, 0xFFFFFFFF); U32(el, 3); U16(el, 20); U16(el, 20); U16(el, 3); U16(el, 0); U16(el, 0); U16(el, 0);
        Attr(el, 6, 0, 0xFFFFFFFF, 0x10, (uint)versionCode);
        Attr(el, 6, 1, 5, 0x03, 5);
        Attr(el, 0xFFFFFFFF, 2, 4, 0x03, 4);

        var body = Concat(pool, map.ToArray(), el.ToArray());
        var file = new MemoryStream();
        U16(file, 0x0003); U16(file, 8); U32(file, (uint)(8 + body.Length));
        file.Write(body);
        return file.ToArray();
    }

    private static void Attr(Stream s, uint ns, uint name, uint raw, byte type, uint data)
    {
        U32(s, ns); U32(s, name); U32(s, raw); U16(s, 8); s.WriteByte(0); s.WriteByte(type); U32(s, data);
    }

    private static byte[] StringPool(string[] strings, bool utf8)
    {
        var data = new MemoryStream();
        var offsets = new List<uint>();
        foreach (var str in strings)
        {
            offsets.Add((uint)data.Length);
            if (utf8)
            {
                var b = Encoding.UTF8.GetBytes(str);
                data.WriteByte((byte)str.Length); data.WriteByte((byte)b.Length); data.Write(b); data.WriteByte(0);
            }
            else
            {
                U16(data, (ushort)str.Length); data.Write(Encoding.Unicode.GetBytes(str)); U16(data, 0);
            }
        }
        while (data.Length % 4 != 0) data.WriteByte(0);
        var headerSize = 28;
        var stringsStart = headerSize + strings.Length * 4;
        var s = new MemoryStream();
        U16(s, 0x0001); U16(s, (ushort)headerSize); U32(s, (uint)(stringsStart + data.Length));
        U32(s, (uint)strings.Length); U32(s, 0); U32(s, utf8 ? 1u << 8 : 0); U32(s, (uint)stringsStart); U32(s, 0);
        foreach (var o in offsets) U32(s, o);
        s.Write(data.ToArray());
        return s.ToArray();
    }

    public static byte[] SigningBlock(uint id, byte[] cert)
    {
        var signedData = Concat(Prefixed([]), Prefixed(Prefixed(cert)), Prefixed([]));
        var signer = Concat(Prefixed(signedData), Prefixed([]), Prefixed([]));
        var value = Prefixed(Prefixed(signer));
        var pair = new MemoryStream();
        U64(pair, (ulong)(4 + value.Length)); U32(pair, id); pair.Write(value);
        var pairs = pair.ToArray();
        var size = (ulong)(pairs.Length + 8 + 16);
        var block = new MemoryStream();
        U64(block, size); block.Write(pairs); U64(block, size);
        block.Write(Encoding.ASCII.GetBytes("APK Sig Block 42"));
        return block.ToArray();
    }

    private static byte[] Splice(byte[] zip, byte[] block)
    {
        var eocd = zip.Length - 22;
        while (BitConverter.ToUInt32(zip, eocd) != 0x06054b50) eocd--;
        var cd = (int)BitConverter.ToUInt32(zip, eocd + 16);
        var result = Concat(zip[..cd], block, zip[cd..]);
        BitConverter.GetBytes((uint)(cd + block.Length)).CopyTo(result, eocd + block.Length + 16);
        return result;
    }

    private static byte[] Prefixed(byte[] b)
    {
        var s = new MemoryStream(); U32(s, (uint)b.Length); s.Write(b); return s.ToArray();
    }

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    private static void U16(Stream s, ushort v) => s.Write(BitConverter.GetBytes(v));
    private static void U32(Stream s, uint v) => s.Write(BitConverter.GetBytes(v));
    private static void U64(Stream s, ulong v) => s.Write(BitConverter.GetBytes(v));
}
