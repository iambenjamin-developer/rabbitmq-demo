using System.Text.Json;
using MassTransit;
using Shared.Contracts.Events;

namespace Notification.Worker.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<ProductUpdated>
    {
        private readonly ILogger<ProductUpdatedConsumer> _logger;

        public ProductUpdatedConsumer(ILogger<ProductUpdatedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductUpdated> context)
        {
            var message = context.Message;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string payload = JsonSerializer.Serialize(context.Message, options);

            _logger.LogInformation($"===ProductUpdated:Consumer===");
            _logger.LogInformation(payload);
        }
    }
}
