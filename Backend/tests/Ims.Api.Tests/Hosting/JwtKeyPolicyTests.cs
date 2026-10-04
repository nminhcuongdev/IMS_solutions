using Ims.Api.Hosting;
using Xunit;

namespace Ims.Api.Tests.Hosting;

public class JwtKeyPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_key(string? key)
    {
        Assert.False(JwtKeyPolicy.IsAcceptableForProduction(key));
    }

    [Fact]
    public void Rejects_a_key_shorter_than_64_characters()
    {
        Assert.False(JwtKeyPolicy.IsAcceptableForProduction(new string('k', 63)));
    }

    [Theory]
    [InlineData("CHANGE-ME-on-prem-demo-key-please-rotate-64-chars-minimum-abcdefghijklmnop")]
    [InlineData("dev-only-secret-please-rotate-in-production-at-least-64-chars-long-string-xxxx")]
    [InlineData("change-me-lowercase-placeholder-that-is-long-enough-to-pass-length-check-xx")]
    public void Rejects_the_placeholder_keys_shipped_in_the_repository(string key)
    {
        Assert.False(JwtKeyPolicy.IsAcceptableForProduction(key));
    }

    [Fact]
    public void Accepts_a_random_key_of_64_characters_or_more()
    {
        var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        Assert.True(JwtKeyPolicy.IsAcceptableForProduction(key));
    }
}
