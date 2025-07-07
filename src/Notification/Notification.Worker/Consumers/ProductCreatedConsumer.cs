using System.Text.Json;
using MassTransit;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    public class ProductCreatedConsumer : IConsumer<ProductCreated>
    {
        private readonly ILogger<ProductCreatedConsumer> _logger;
        private readonly NotificationDbContext _context;
        private readonly IEmailService _emailService;

        public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger, NotificationDbContext context, IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<ProductCreated> context)
        {
            var retryAttempt = context.GetRetryAttempt();
            if (context.Message.Category == "Error simulado") // Simular fallos en los primeros 3 intentos
            {
                _logger.LogWarning($"=== Intento #{retryAttempt} de procesar el Product {context.Message.Name} ===");
                throw new Exception("=== Fallo simulado para probar reintentos ===");
            }

            var message = context.Message;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            // Aquí podrías guardar en DB o enviar notificaciones reales
            var log = new InventoryEventLog
            {
                EventType = nameof(ProductCreated),
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(log);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(
                       subject: $"Producto '{context.Message.Name}' creado",
                       bodyHtml: $"<h1>Detalles</h1><p>{payload}</p>"
                       );

            _logger.LogInformation($"===ProductCreated:Consumer===");
            _logger.LogInformation(payload);

        }
    }
}
