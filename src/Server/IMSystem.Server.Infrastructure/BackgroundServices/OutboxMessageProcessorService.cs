using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Common; // For DomainEvent (if needed for casting, though INotification is primary)
using IMSystem.Server.Domain.Entities; // For OutboxMessage
using IMSystem.Server.Infrastructure.Configuration; // 添加这一行
using IMSystem.Server.Infrastructure.Persistence; // For ApplicationDbContext
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // 添加这一行

namespace IMSystem.Server.Infrastructure.BackgroundServices;

public class OutboxMessageProcessorService : BackgroundService
{
    private readonly ILogger<OutboxMessageProcessorService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxProcessorSettings _settings;

    public OutboxMessageProcessorService(
        ILogger<OutboxMessageProcessorService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxProcessorSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxMessageProcessorService is starting.");

        stoppingToken.Register(() => _logger.LogInformation("OutboxMessageProcessorService is stopping."));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred in OutboxMessageProcessorService loop.");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("OutboxMessageProcessorService has stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("OutboxMessageProcessorService: Polling for new outbox messages.");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messagesToProcess = await dbContext.OutboxMessages
            .Where(om => om.ProcessedAt == null && om.RetryCount < _settings.MaxRetryCount)
            .OrderBy(om => om.OccurredAt)
            .Take(_settings.BatchSize) // Process messages in batches
            .ToListAsync(stoppingToken);

        if (!messagesToProcess.Any())
        {
            _logger.LogDebug("OutboxMessageProcessorService: No new messages to process.");
            return;
        }

        _logger.LogInformation("OutboxMessageProcessorService: Found {Count} messages to process.", messagesToProcess.Count);

        foreach (var message in messagesToProcess)
        {
            if (stoppingToken.IsCancellationRequested) break;

            _logger.LogInformation("OutboxMessageProcessorService: Processing message ID {MessageId}, Type {EventType}.", message.Id, message.EventType);
            
            try
            {
                Type? eventType = Type.GetType(message.EventType); 

                if (eventType == null)
                {
                    _logger.LogError("OutboxMessageProcessorService: Could not resolve event type '{EventType}' for message ID {MessageId}.", message.EventType, message.Id);
                    message.MarkAsFailed($"Could not resolve event type: {message.EventType}"); // Terminal failure for this message
                }
                else
                {
                    var domainEvent = JsonSerializer.Deserialize(message.EventPayload, eventType, new JsonSerializerOptions());

                    if (domainEvent is INotification notificationEvent)
                    {
                        await mediator.Publish(notificationEvent, stoppingToken);
                        message.MarkAsProcessed();
                        _logger.LogInformation("OutboxMessageProcessorService: Successfully processed and published event for message ID {MessageId}.", message.Id);
                    }
                    else
                    {
                        _logger.LogError("OutboxMessageProcessorService: Deserialized event for message ID {MessageId} of type {EventType} is not an INotification.", message.Id, message.EventType);
                        message.MarkAsFailed($"Deserialized event {message.EventType} is not an INotification."); // Terminal failure
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "OutboxMessageProcessorService: JSON deserialization failed for message ID {MessageId}, Type {EventType}.", message.Id, message.EventType);
                message.IncrementRetryCount();
                if (message.RetryCount >= _settings.MaxRetryCount)
                {
                    message.MarkAsFailed($"JSON Deserialization Error after {message.RetryCount} retries: {jsonEx.Message}");
                    _logger.LogWarning("OutboxMessageProcessorService: Message ID {MessageId} (Type: {EventType}) failed after max retries due to JSON error.", message.Id, message.EventType);
                }
                // If not max retries, the error is logged, RetryCount incremented. The specific error for this attempt isn't stored on OutboxMessage.Error yet.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxMessageProcessorService: Error processing message ID {MessageId}, Type {EventType}.", message.Id, message.EventType);
                message.IncrementRetryCount();
                if (message.RetryCount >= _settings.MaxRetryCount)
                {
                    message.MarkAsFailed($"Error processing message after {message.RetryCount} retries: {ex.Message}");
                     _logger.LogWarning("OutboxMessageProcessorService: Message ID {MessageId} (Type: {EventType}) failed after max retries.", message.Id, message.EventType);
                }
                // If not max retries, error logged, RetryCount incremented.
            }
        }
        await dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("OutboxMessageProcessorService: Finished processing batch of {Count} messages.", messagesToProcess.Count);
    }
}