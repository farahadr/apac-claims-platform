using System.Text.Json;
using ClaimsApi.Infrastructure.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ClaimsApi.Features.Claims;

// Sends ClaimStatusChanged messages to Kafka. One instance for the whole app
// (a Kafka producer is meant to be shared, not created per request).
public sealed class ClaimEventPublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<ClaimEventPublisher> _logger;

    public ClaimEventPublisher(IOptions<KafkaOptions> options, ILogger<ClaimEventPublisher> logger)
    {
        _topic = options.Value.Topic;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            EnableIdempotence = true,  // Kafka won't store the same message twice on retries
            MessageTimeoutMs = 120_000, // keep retrying in the background for up to 2 minutes
        }).Build();
    }

    // Called only AFTER the database save succeeded.
    // Doesn't wait for Kafka: the message is queued and sent in the background,
    // so a slow or restarting broker never slows down the API request.
    public void Publish(ClaimStatusChanged message)
    {
        // Key = claim id: all messages for one claim go to the same partition,
        // so the consumer always sees them in the right order.
        var kafkaMessage = new Message<string, string>
        {
            Key = message.ClaimId.ToString(),
            Value = JsonSerializer.Serialize(message, ClaimStatusChanged.Json),
        };

        try
        {
            // The callback runs later, once Kafka confirms (or we gave up after 2 minutes).
            _producer.Produce(_topic, kafkaMessage, report =>
            {
                if (report.Error.IsError)
                    LogLost(message, report.Error.Reason);
            });
        }
        catch (KafkaException ex)
        {
            // e.g. the local send queue is full.
            LogLost(message, ex.Error.Reason);
        }
    }

    // The claim is already saved, so we never fail the request.
    // The dashboard will miss this change (see notes on the outbox pattern).
    private void LogLost(ClaimStatusChanged m, string reason) =>
        _logger.LogError("Could not publish {ToStatus} for claim {ClaimId}: {Reason}", m.ToStatus, m.ClaimId, reason);

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5)); // send anything still waiting
        _producer.Dispose();
    }
}
