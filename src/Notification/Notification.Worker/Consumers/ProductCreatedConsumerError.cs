using System.Text.Json;
using MassTransit;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    /// <summary>
    /// Consumer para procesar mensajes que han fallado después de todos los reintentos
    /// Este consumer se ejecuta cuando un mensaje termina en la cola de error
    /// </summary>
    public class ProductCreatedConsumerError : IConsumer<ProductCreated>
    {
        private readonly ILogger<ProductCreatedConsumerError> _logger;
        private readonly NotificationDbContext _context;
        private readonly IEmailService _emailService;

        public ProductCreatedConsumerError(ILogger<ProductCreatedConsumerError> logger, NotificationDbContext context, IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<ProductCreated> context)
        {
            var message = context.Message;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            // Guardar en tabla específica de errores
            var errorLog = new ErrorLog
            {
                EventType = nameof(ProductCreated),
                OriginalQueue = "product-created-queue",
                RoutingKey = "product.created",
                Payload = payload,
                RetryAttempts = 3,
                ErrorMessage = "Mensaje falló después de todos los reintentos",
                ErrorTime = DateTime.UtcNow,
                IsResolved = false
            };

            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(
                             subject: $"Error: Producto '{context.Message.Name}' no creado",
                             bodyHtml: $"<h1>Detalles</h1><p>{payload}</p>"
                             );

            _logger.LogError($"===ProductCreatedConsumerError:Consumer===");
            _logger.LogError(payload);
        }
    }
}