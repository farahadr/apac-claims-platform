using System.Text.Json;
using ClaimsApi.Data;
using ClaimsApi.Features.Claims;
using ClaimsApi.Infrastructure.Kafka;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;

namespace ClaimsApi.Features.Dashboard;

// Runs in the background for as long as the app runs. Reads every message from
// the "claim-events" topic and updates the DashboardClaims table.
public sealed class ClaimEventsConsumer(
    IServiceScopeFactory scopes,
    IOptions<KafkaOptions> options,
    ILogger<ClaimEventsConsumer> logger) : BackgroundService
{
    private readonly KafkaOptions _kafka = options.Value;

    // Consume() blocks while waiting for messages, so run it on its own thread.
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => RunAsync(stoppingToken), stoppingToken);

    private async Task RunAsync(CancellationToken ct)
    {
        await EnsureTopicExistsAsync();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            GroupId = "claims-dashboard",            // Kafka remembers how far this group has read
            AutoOffsetReset = AutoOffsetReset.Earliest, // first run: start from the oldest message
            EnableAutoCommit = false,                // we mark a message done only after saving it
        }).Build();

        consumer.Subscribe(_kafka.Topic);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;
                try
                {
                    result = consumer.Consume(ct);
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    continue;
                }

                var message = TryRead(result.Message.Value);
                if (message is null)
                {
                    // Broken message: retrying won't fix it, so log it and move on.
                    logger.LogError("Skipping unreadable message at {Offset}", result.TopicPartitionOffset);
                    consumer.Commit(result);
                    continue;
                }

                try
                {
                    await ApplyAsync(message, ct);
                    consumer.Commit(result); // done: don't send me this one again
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // e.g. database down. Rewind to this message and try again shortly.
                    logger.LogError(ex, "Could not apply message for claim {ClaimId}, retrying", message.ClaimId);
                    consumer.Seek(result.TopicPartitionOffset);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
        finally
        {
            consumer.Close(); // leave the group cleanly
        }
    }

    private static ClaimStatusChanged? TryRead(string json)
    {
        try { return JsonSerializer.Deserialize<ClaimStatusChanged>(json, ClaimStatusChanged.Json); }
        catch (JsonException) { return null; }
    }

    private async Task ApplyAsync(ClaimStatusChanged message, CancellationToken ct)
    {
        // A background service has no request, so create a scope to get a DbContext.
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();

        var row = await db.DashboardClaims.FindAsync([message.ClaimId], ct);
        if (row is null)
        {
            row = new DashboardClaim { ClaimId = message.ClaimId };
            db.DashboardClaims.Add(row);
        }
        else if (message.ChangedAt <= row.LastChangedAt)
        {
            return; // older than (or same as) what we already have: ignore
        }

        row.Status = message.ToStatus;
        row.EstimatedAmount = message.EstimatedAmount;
        row.AssignedOfficer = message.AssignedOfficer;
        row.LastChangedAt = message.ChangedAt;
        await db.SaveChangesAsync(ct);
    }

    // Create the topic on startup if it isn't there yet.
    private async Task EnsureTopicExistsAsync()
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
        }).Build();
        try
        {
            await admin.CreateTopicsAsync(
                [new TopicSpecification { Name = _kafka.Topic, NumPartitions = 3, ReplicationFactor = 1 }],
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });
            logger.LogInformation("Created Kafka topic {Topic}", _kafka.Topic);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // Already there: nothing to do.
        }
        catch (Exception ex)
        {
            // Kafka may still be starting. Carry on; subscribing will keep retrying.
            logger.LogWarning(ex, "Could not create Kafka topic {Topic}", _kafka.Topic);
        }
    }
}
