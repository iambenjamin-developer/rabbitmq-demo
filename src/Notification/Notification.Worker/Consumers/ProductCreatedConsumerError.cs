using System.Text.Json;
using MassTransit;
using Notification.Application.Interfaces;
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
        private readonly IEmailService _emailService;

        public ProductCreatedConsumerError(ILogger<ProductCreatedConsumerError> logger, IEmailService emailService)
        {
            _logger = logger;
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

            await _emailService.SendEmailAsync(
                    subject: $"Error: Producto '{context.Message.Name}' no creado",
                    bodyHtml: $"<h1>Detalles</h1><p>{payload}</p>"
                    );

            _logger.LogError($"===ProductCreatedConsumerError:Consumer===");
            _logger.LogError(payload);
        }
    }
}