using System.Text.Json;
using Confluent.Kafka;
using FinancialTracker.Accounts.Application.Contracts;

namespace FinancialTracker.Accounts.API.Kafka;

public sealed class TransactionEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<TransactionEventConsumer> _logger;
    private const string TopicName = "transaction-events";

    public TransactionEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConsumer<string, string> consumer,
        ILogger<TransactionEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give Kafka time to finish initializing (supplements docker-compose healthcheck)
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken ct)
    {
        _consumer.Subscribe(TopicName);
        _logger.LogInformation("Kafka consumer subscribed to topic '{Topic}'", TopicName);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(TimeSpan.FromSeconds(3));
                if (result is null)
                    continue;

                _logger.LogInformation(
                    "Received Kafka message from topic '{Topic}' partition {Partition} offset {Offset}",
                    result.Topic, result.Partition.Value, result.Offset.Value);

                await ProcessEvent(result.Message.Value, ct);

                _consumer.Commit(result);
            }
            catch (ConsumeException ex) when (ex.Error.Code == Confluent.Kafka.ErrorCode.UnknownTopicOrPart)
            {
                _logger.LogWarning("Topic '{Topic}' does not exist yet — waiting 5s before retry", TopicName);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming Kafka message");
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing consumed event");
            }
        }

        _consumer.Close();
    }

    private async Task ProcessEvent(string payload, CancellationToken ct)
    {
        var eventData = JsonSerializer.Deserialize<TransactionCreatedEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (eventData is null)
        {
            _logger.LogWarning("Failed to deserialize event payload: {Payload}", payload);
            return;
        }

        _logger.LogInformation(
            "Processing TransactionCreated event: TransactionId={TransactionId}, AccountId={AccountId}, Type={Type}, Amount={Amount}",
            eventData.TransactionId, eventData.AccountId, eventData.TransactionType, eventData.Amount);

        using var scope = _scopeFactory.CreateScope();
        var applier = scope.ServiceProvider.GetRequiredService<ILedgerTransactionApplier>();
        var ledgerEvent = new TransactionLedgerEvent(
            eventData.TransactionId,
            eventData.AccountId,
            eventData.UserId,
            eventData.TransactionType,
            eventData.Amount);

        var result = await applier.ApplyAsync(ledgerEvent, ct);

        switch (result)
        {
            case LedgerApplyResult.Applied:
                _logger.LogInformation(
                    "Ledger applied for TransactionId={TransactionId}, AccountId={AccountId}, Type={Type}, Amount={Amount}",
                    eventData.TransactionId, eventData.AccountId, eventData.TransactionType, eventData.Amount);
                break;
            case LedgerApplyResult.AlreadyProcessed:
                _logger.LogInformation("Skipped duplicate TransactionId={TransactionId} (already processed)", eventData.TransactionId);
                break;
            case LedgerApplyResult.AccountNotFound:
                _logger.LogWarning(
                    "Ledger event not applied — account not found or access denied (TransactionId={TransactionId}, AccountId={AccountId})",
                    eventData.TransactionId, eventData.AccountId);
                break;
            case LedgerApplyResult.InsufficientBalance:
                _logger.LogWarning(
                    "Ledger event not applied — insufficient balance (TransactionId={TransactionId}, AccountId={AccountId})",
                    eventData.TransactionId, eventData.AccountId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }

    private sealed class TransactionCreatedEvent
    {
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public string? Note { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
