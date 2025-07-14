using System.Text.Json;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Inventory.Application.Services
{
    public class ResilientMessagePublisher : IResilientMessagePublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IPendingMessageService _pendingMessageService;
        private readonly IResiliencePolicy _resiliencePolicy;
        private readonly ILogger<ResilientMessagePublisher> _logger;

        public ResilientMessagePublisher(
            IPublishEndpoint publishEndpoint,
            IPendingMessageService pendingMessageService,
            IResiliencePolicy resiliencePolicy,
            ILogger<ResilientMessagePublisher> logger)
        {
            _publishEndpoint = publishEndpoint;
            _pendingMessageService = pendingMessageService;
            _resiliencePolicy = resiliencePolicy;
            _logger = logger;
        }

        public async Task PublishWithResilienceAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                // Intentar publicar directamente con políticas de resiliencia
                await _resiliencePolicy.ExecuteAsync(async (ct) =>
                {
                    await _publishEndpoint.Publish(message, publishCtx =>
                    {
                        publishCtx.SetRoutingKey(routingKey);
                    }, ct);
                }, cancellationToken);

                _logger.LogInformation("Mensaje publicado exitosamente: {EventType} con routing key {RoutingKey}",
                    typeof(T).Name, routingKey);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning("Timeout al publicar mensaje {EventType}. Guardando como pendiente.", typeof(T).Name);
                await SavePendingMessageAsync(message, routingKey, ex.Message);
                throw; // Re-lanzar para que el controlador maneje la respuesta HTTP
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning("Circuito abierto al publicar mensaje {EventType}. Guardando como pendiente.", typeof(T).Name);
                await SavePendingMessageAsync(message, routingKey, ex.Message);
                throw; // Re-lanzar para que el controlador maneje la respuesta HTTP
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al publicar mensaje {EventType}. Guardando como pendiente.", typeof(T).Name);
                await SavePendingMessageAsync(message, routingKey, ex.Message);
                throw; // Re-lanzar para que el controlador maneje la respuesta HTTP
            }
        }

        private async Task SavePendingMessageAsync<T>(T message, string routingKey, string errorMessage) where T : class
        {
            try
            {
                var payload = JsonSerializer.Serialize(message);
                var messageId = await _pendingMessageService.SavePendingMessageAsync(
                    typeof(T).Name,
                    routingKey,
                    payload);

                _logger.LogInformation("Mensaje guardado como pendiente con ID {MessageId}. Error: {Error}",
                    messageId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar mensaje pendiente. El mensaje se perderá.");
                throw; // Si no podemos guardar el mensaje pendiente, es un error crítico
            }
        }
    }
}
