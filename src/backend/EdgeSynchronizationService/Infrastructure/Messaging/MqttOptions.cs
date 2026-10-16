namespace EdgeSynchronizationService.Infrastructure.Messaging;

public class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string Subscription { get; set; } = "minesense/+/sync";
}
