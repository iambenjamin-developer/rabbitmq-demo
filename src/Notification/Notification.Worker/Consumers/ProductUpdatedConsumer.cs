using System.Text.Json;
using MassTransit;
using Notification.Application.Interfaces;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<ProductUpdated>
    {
        private readonly ILogger<ProductUpdatedConsumer> _logger;
        private readonly IEmailService _emailService;

        public ProductUpdatedConsumer(ILogger<ProductUpdatedConsumer> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<ProductUpdated> context)
        {
            var message = context.Message;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            await _emailService.SendEmailAsync(
                    subject: $"Producto '{context.Message.Name}' actualizado",
                    bodyHtml: $"<h1>Detalles</h1><p>{payload}</p>"
                    );

            _logger.LogInformation($"===ProductUpdated:Consumer===");
            _logger.LogInformation(payload);
        }
    }
}
