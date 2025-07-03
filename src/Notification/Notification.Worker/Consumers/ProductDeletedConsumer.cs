using System.Text.Json;
using MassTransit;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    public class ProductDeletedConsumer : IConsumer<ProductDeleted>
    {
        private readonly ILogger<ProductDeletedConsumer> _logger;

        public ProductDeletedConsumer(ILogger<ProductDeletedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductDeleted> context)
        {
            var message = context.Message;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            _logger.LogInformation($"===ProductDeleted:Consumer===");
            _logger.LogInformation(payload);
        }
    }
}
