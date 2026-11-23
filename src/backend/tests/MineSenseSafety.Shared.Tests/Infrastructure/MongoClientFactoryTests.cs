using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;

namespace MineSenseSafety.Shared.Tests.Infrastructure;

public sealed class MongoClientFactoryTests : IDisposable
{
    private readonly string _bundlePath = Path.GetTempFileName();

    public void Dispose() => File.Delete(_bundlePath);

    private static X509Certificate2 CreateAuthority(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private static X509Certificate2 CreateServerCertificate(X509Certificate2 authority)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=docdb.test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var serial = RandomNumberGenerator.GetBytes(8);
        return request.Create(authority, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(10), serial);
    }

    private static X509Certificate2 CreateIntermediate(X509Certificate2 authority, string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        var serial = RandomNumberGenerator.GetBytes(8);
        using var issued = request.Create(authority, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(20), serial);
        return issued.CopyWithPrivateKey(key);
    }

    [Fact]
    public void Validator_AcceptsIntermediatePresentedByTheServer()
    {
        // Arrange: the bundle has the root only, as with the Amazon subordinate CAs sent in the handshake.
        using var root = CreateAuthority("Bundle CA");
        File.WriteAllText(_bundlePath, root.ExportCertificatePem());
        using var intermediate = CreateIntermediate(root, "Subordinate CA");
        using var server = CreateServerCertificate(intermediate);
        using var presented = new X509Chain();
        presented.ChainPolicy.ExtraStore.Add(intermediate);

        // Act
        var accepted = MongoClientFactory.CreateCertificateValidator(_bundlePath)(this, server, presented, SslPolicyErrors.RemoteCertificateChainErrors);

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public void Validator_RejectsRootPresentedByTheServer()
    {
        // Arrange: a server cannot become trusted by sending its own authority.
        using var trusted = CreateAuthority("Bundle CA");
        using var other = CreateAuthority("Other CA");
        File.WriteAllText(_bundlePath, trusted.ExportCertificatePem());
        using var server = CreateServerCertificate(other);
        using var presented = new X509Chain();
        presented.ChainPolicy.ExtraStore.Add(other);

        // Act
        var accepted = MongoClientFactory.CreateCertificateValidator(_bundlePath)(this, server, presented, SslPolicyErrors.RemoteCertificateChainErrors);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void Validator_AcceptsCertificateSignedByTheBundle()
    {
        // Arrange
        using var authority = CreateAuthority("Bundle CA");
        File.WriteAllText(_bundlePath, authority.ExportCertificatePem());
        using var server = CreateServerCertificate(authority);

        // Act
        var accepted = MongoClientFactory.CreateCertificateValidator(_bundlePath)(this, server, null, SslPolicyErrors.RemoteCertificateChainErrors);

        // Assert: the chain error of the system trust store does not matter, the bundle decides.
        Assert.True(accepted);
    }

    [Fact]
    public void Validator_RejectsCertificateOfAnotherAuthority()
    {
        // Arrange
        using var trusted = CreateAuthority("Bundle CA");
        using var other = CreateAuthority("Other CA");
        File.WriteAllText(_bundlePath, trusted.ExportCertificatePem());
        using var server = CreateServerCertificate(other);

        // Act
        var accepted = MongoClientFactory.CreateCertificateValidator(_bundlePath)(this, server, null, SslPolicyErrors.None);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void Validator_RejectsNameMismatch()
    {
        // Arrange
        using var authority = CreateAuthority("Bundle CA");
        File.WriteAllText(_bundlePath, authority.ExportCertificatePem());
        using var server = CreateServerCertificate(authority);

        // Act
        var accepted = MongoClientFactory.CreateCertificateValidator(_bundlePath)(this, server, null, SslPolicyErrors.RemoteCertificateNameMismatch);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public void Create_WithMissingBundle_FailsWithAClearMessage()
    {
        // Act
        var error = Assert.Throws<InvalidOperationException>(() =>
            MongoClientFactory.Create("mongodb://localhost:27017", Path.Combine(Path.GetTempPath(), "missing-bundle.pem")));

        // Assert
        Assert.Contains(MongoClientFactory.TlsCaFileKey, error.Message);
    }

    [Fact]
    public void Create_ConnectionStringOfDocumentDb_DisablesRetryableWrites()
    {
        // Arrange
        using var authority = CreateAuthority("Bundle CA");
        File.WriteAllText(_bundlePath, authority.ExportCertificatePem());

        // Act
        var client = MongoClientFactory.Create(
            "mongodb://user:pass@docdb.test:27017/?tls=true&replicaSet=rs0&readPreference=primary&retryWrites=false&authMechanism=SCRAM-SHA-1",
            _bundlePath);

        // Assert
        Assert.False(client.Settings.RetryWrites);
        Assert.True(client.Settings.UseTls);
        Assert.NotNull(client.Settings.SslSettings?.ServerCertificateValidationCallback);
    }
}
