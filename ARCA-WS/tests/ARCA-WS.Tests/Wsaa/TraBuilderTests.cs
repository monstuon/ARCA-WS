using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ARCA_WS.Infrastructure.Wsaa;

namespace ARCA_WS.Tests.Wsaa;

public sealed class TraBuilderTests
{
    [Fact]
    public void BuildUnsignedTra_ShouldIncludeServiceName()
    {
        var sut = new TraBuilder();

        var tra = sut.BuildUnsignedTra("wsfe", 120);

        Assert.Contains("<service>wsfe</service>", tra, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SignTra_ShouldReturnBase64Cms()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=arca-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var sut = new TraBuilder();
        var tra = sut.BuildUnsignedTra("wsfe", 120);

        var cms = sut.SignTra(tra, cert);

        Assert.False(string.IsNullOrWhiteSpace(cms));
        Assert.True(Convert.TryFromBase64String(cms, new Span<byte>(new byte[cms.Length]), out _));
    }
}
