using Confluent.Kafka;
using FinancialTracker.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Transactions.API.Outbox;

public sealed class OutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const string TopicName = "transaction-events";
    private const int BatchSize = 50;

    public OutboxRelayService(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox relay service started. Polling every {Interval}s", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Found {Count} unprocessed outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var kafkaMessage = new Message<string, string>
                {
                    Key = message.Id.ToString(),
                    Value = message.Payload,
                    Headers = new Headers
                    {
                        { "eventType", System.Text.Encoding.UTF8.GetBytes(message.EventType) },
                        { "eventId", System.Text.Encoding.UTF8.GetBytes(message.Id.ToString()) }
                    }
                };

                var result = await _producer.ProduceAsync(TopicName, kafkaMessage, ct);

                _logger.LogInformation(
                    "Published outbox message {MessageId} to Kafka topic '{Topic}' partition {Partition} offset {Offset}",
                    message.Id, TopicName, result.Partition.Value, result.Offset.Value);

                message.ProcessedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish outbox message {MessageId} to Kafka — will retry on next poll",
                    message.Id);
                break;
            }
        }
    }
}
