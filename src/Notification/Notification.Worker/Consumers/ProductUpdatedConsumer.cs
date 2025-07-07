using System.Text.Json;
using MassTransit;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<ProductUpdated>
    {
        private readonly ILogger<ProductUpdatedConsumer> _logger;
        private readonly NotificationDbContext _context;
        private readonly IEmailService _emailService;

        public ProductUpdatedConsumer(ILogger<ProductUpdatedConsumer> logger, NotificationDbContext context, IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<ProductUpdated> context)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            var log = new InventoryEventLog
            {
                EventType = nameof(ProductUpdated),
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            _context.InventoryEventLogs.Add(log);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(
                     subject: $"Producto '{context.Message.Name}' actualizado",
                     bodyHtml: $"<h1>Detalles</h1><p>{payload}</p>"
                     );

            _logger.LogInformation($"===ProductUpdated:Consumer===");
            _logger.LogInformation(payload);
        }
    }
}
