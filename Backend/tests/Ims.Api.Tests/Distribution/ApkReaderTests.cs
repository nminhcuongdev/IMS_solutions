using System.Security.Cryptography;
using Ims.Api.Features.Distribution;
using Xunit;

namespace Ims.Api.Tests.Distribution;

public class ApkReaderTests
{
    private static string Fp(byte[] cert) => ApkReader.FormatFingerprint(SHA256.HashData(cert));

    [Fact]
    public void Reads_package_version_and_v2_signer_fingerprint()
    {
        var r = ApkReader.Read(ApkBuilder.Build(versionCode: 10500, versionName: "1.5.0"));

        Assert.Null(r.Failure);
        Assert.Equal("com.imssolutions.ht", r.PackageName);
        Assert.Equal(10500, r.VersionCode);
        Assert.Equal("1.5.0", r.VersionName);
        Assert.Equal(Fp(ApkBuilder.ReleaseCert), r.SignerFingerprint);
    }

    [Fact]
    public void Reads_a_v3_signing_block_too()
    {
        var r = ApkReader.Read(ApkBuilder.Build(scheme: ApkBuilder.V3, cert: ApkBuilder.DebugCert));

        Assert.Null(r.Failure);
        Assert.Equal(Fp(ApkBuilder.DebugCert), r.SignerFingerprint);
    }

    [Fact]
    public void Reads_a_utf8_string_pool()
    {
        var r = ApkReader.Read(ApkBuilder.Build(package: "com.example.app", versionName: "2.0.1", utf8: true));

        Assert.Equal("com.example.app", r.PackageName);
        Assert.Equal("2.0.1", r.VersionName);
    }

    [Fact]
    public void Fingerprint_is_colon_separated_upper_hex()
    {
        var fp = ApkReader.FormatFingerprint([0x0a, 0xff, 0x10]);

        Assert.Equal("0A:FF:10", fp);
    }

    [Fact]
    public void A_file_that_is_not_a_zip_fails_as_zip()
    {
        var r = ApkReader.Read("this is not an apk"u8.ToArray());

        Assert.Equal("zip", r.Failure);
    }

    [Fact]
    public void A_zip_without_a_manifest_fails_as_manifest()
    {
        var r = ApkReader.Read(ApkBuilder.Build(withManifest: false));

        Assert.Equal("manifest", r.Failure);
    }

    [Fact]
    public void An_apk_without_a_v2_or_v3_block_fails_as_signature()
    {
        var r = ApkReader.Read(ApkBuilder.Build(scheme: null));

        Assert.Equal("signature", r.Failure);
        Assert.Equal("com.imssolutions.ht", r.PackageName);
        Assert.Null(r.SignerFingerprint);
    }

    [Fact]
    public void A_truncated_manifest_fails_without_throwing()
    {
        var r = ApkReader.ReadManifest(ApkBuilder.Manifest("p", 1, "1", false)[..40]);

        Assert.Null(r);
    }
}
