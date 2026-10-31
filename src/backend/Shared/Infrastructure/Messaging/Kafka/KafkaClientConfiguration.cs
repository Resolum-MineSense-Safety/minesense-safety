using AWS.MSK.Auth;
using Confluent.Kafka;

namespace MineSenseSafety.Shared.Infrastructure.Messaging.Kafka;

/// <summary>
/// Connection settings shared by producers, consumers and the admin client: plaintext locally
/// (Docker Compose) and SASL/OAUTHBEARER with IAM tokens for MSK Serverless (port 9098).
/// </summary>
internal static class KafkaClientConfiguration
{
    private static readonly AWSMSKAuthTokenGenerator TokenGenerator = new();

    public static void Apply(ClientConfig config, KafkaOptions options, string clientId)
    {
        config.BootstrapServers = options.BootstrapServers;
        config.ClientId = clientId;
        if (!options.UsesAwsMskIam) return;

        config.SecurityProtocol = SecurityProtocol.SaslSsl;
        config.SaslMechanism = SaslMechanism.OAuthBearer;
    }

    /// <summary>Signs a fresh MSK IAM token with the pod credentials (IRSA) whenever librdkafka asks.</summary>
    public static Action<IClient, string>? TokenRefreshHandler(KafkaOptions options)
    {
        if (!options.UsesAwsMskIam) return null;
        var region = Amazon.RegionEndpoint.GetBySystemName(options.AwsRegion);
        return (client, _) =>
        {
            try
            {
                var (token, expiryMs) = TokenGenerator.GenerateAuthToken(region);
                client.OAuthBearerSetToken(token, expiryMs, "minesense");
            }
            catch (Exception exception)
            {
                client.OAuthBearerSetTokenFailure(exception.Message);
            }
        };
    }
}
