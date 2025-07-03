using System.Text.Json;
using MassTransit;
using Notification.Application.Interfaces;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    public class ProductDeletedConsumer : IConsumer<ProductDeleted>
    {
        private readonly ILogger<ProductDeletedConsumer> _logger;
        private readonly IEmailService _emailService;

        public ProductDeletedConsumer(ILogger<ProductDeletedConsumer> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Consume(ConsumeContext<ProductDeleted> context)
        {
            var message = context.Message;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            await _emailService.SendEmailAsync(
                    subject: $"Producto  con Id: {context.Message.Id} eliminado",
                    bodyHtml: $"<h1>Detalles</h1><p>{payload}</p>"
                    );


            _logger.LogInformation($"===ProductDeleted:Consumer===");
            _logger.LogInformation(payload);
        }
    }
}
