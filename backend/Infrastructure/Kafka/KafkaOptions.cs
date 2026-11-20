namespace ClaimsApi.Infrastructure.Kafka;

// Filled from the "Kafka" section of appsettings.json.
public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "claim-events";
}
