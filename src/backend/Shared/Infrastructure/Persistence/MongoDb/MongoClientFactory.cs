using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;

/// <summary>
/// Builds the MongoDB client of a service. Amazon DocumentDB presents a certificate signed by the
/// Amazon RDS certificate authorities, which are not in the public trust store, and the .NET driver
/// ignores <c>tlsCAFile</c>. With <c>MongoDb:TlsCaFile</c> (the PEM bundle Amazon publishes) the
/// server certificate is validated against that bundle only, without changing the trust of any other
/// TLS connection of the process (STS, MSK, HTTP).
/// </summary>
public static class MongoClientFactory
{
    public const string TlsCaFileKey = "MongoDb:TlsCaFile";

    public static IMongoClient Create(string connectionString, string? tlsCaFile)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        if (!string.IsNullOrWhiteSpace(tlsCaFile))
            settings.SslSettings = new SslSettings
            {
                ServerCertificateValidationCallback = CreateCertificateValidator(tlsCaFile)
            };
        return new MongoClient(settings);
    }

    /// <summary>Accepts a certificate only if its name matches and it chains to a root of the PEM bundle.</summary>
    public static RemoteCertificateValidationCallback CreateCertificateValidator(string pemBundlePath)
    {
        if (!File.Exists(pemBundlePath))
            throw new InvalidOperationException(
                $"{TlsCaFileKey} points to '{pemBundlePath}', which does not exist. Mount the CA bundle or clear the setting.");

        var bundle = new X509Certificate2Collection();
        bundle.ImportFromPemFile(pemBundlePath);
        if (bundle.Count == 0)
            throw new InvalidOperationException($"{TlsCaFileKey}: '{pemBundlePath}' has no certificates.");
        // The bundle mixes self-signed roots with intermediates; only the roots are trust anchors.
        var roots = bundle.Where(certificate => certificate.SubjectName.RawData.SequenceEqual(certificate.IssuerName.RawData)).ToArray();
        var intermediates = bundle.Except(roots).ToArray();

        return (_, certificate, _, errors) =>
        {
            if (certificate is null || errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)) return false;

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(roots);
            chain.ChainPolicy.ExtraStore.AddRange(intermediates);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return chain.Build(certificate as X509Certificate2 ?? new X509Certificate2(certificate));
        };
    }
}
