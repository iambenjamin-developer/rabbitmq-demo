using Inventory.Application.Interfaces;
using Inventory.Domain.Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using System.Text.Json;

namespace Inventory.Application.Services
{
    public class PendingMessageProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PendingMessageProcessorService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30); // Procesar cada 30 segundos
        private readonly int _maxRetries = 5;

        public PendingMessageProcessorService(
            IServiceProvider serviceProvider,
            ILogger<PendingMessageProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de procesamiento de mensajes pendientes iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingMessagesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando mensajes pendientes");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }
        }

        private async Task ProcessPendingMessagesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var pendingMessageService = scope.ServiceProvider.GetRequiredService<IPendingMessageService>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var pendingMessages = await pendingMessageService.GetPendingMessagesAsync();

            if (!pendingMessages.Any())
            {
                return;
            }

            _logger.LogInformation("Procesando {Count} mensajes pendientes", pendingMessages.Count());

            foreach (var message in pendingMessages)
            {
                if (message.RetryCount >= _maxRetries)
                {
                    _logger.LogWarning("Mensaje {MessageId} excedió el máximo de reintentos ({MaxRetries})",
                        message.Id, _maxRetries);
                    continue;
                }

                try
                {
                    await ProcessMessageAsync(message, publishEndpoint, pendingMessageService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando mensaje {MessageId}", message.Id);
                    await pendingMessageService.IncrementRetryCountAsync(message.Id, ex.Message);
                }
            }
        }

        private async Task ProcessMessageAsync(
            PendingMessage message,
            IPublishEndpoint publishEndpoint,
            IPendingMessageService pendingMessageService)
        {
            // Deserializar el payload según el tipo de evento
            var eventObject = DeserializeMessage(message.EventType, message.Payload);

            if (eventObject == null)
            {
                _logger.LogError("No se pudo deserializar el mensaje {MessageId} de tipo {EventType}",
                    message.Id, message.EventType);
                await pendingMessageService.IncrementRetryCountAsync(message.Id, "Error de deserialización");
                return;
            }

            // Publicar el mensaje
            await publishEndpoint.Publish(eventObject, publishCtx =>
            {
                publishCtx.SetRoutingKey(message.RoutingKey);
            });

            // Marcar como procesado
            await pendingMessageService.MarkAsProcessedAsync(message.Id);

            _logger.LogInformation("Mensaje {MessageId} procesado exitosamente", message.Id);
        }

        private object? DeserializeMessage(string eventType, string payload)
        {
            try
            {
                return eventType switch
                {
                    nameof(ProductCreated) =>
                        JsonSerializer.Deserialize<ProductCreated>(payload),
                    nameof(ProductUpdated) =>
                        JsonSerializer.Deserialize<ProductUpdated>(payload),
                    nameof(ProductDeleted) =>
                        JsonSerializer.Deserialize<ProductDeleted>(payload),
                    _ => null
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializando mensaje de tipo {EventType}", eventType);
                return null;
            }
        }
    }
}
