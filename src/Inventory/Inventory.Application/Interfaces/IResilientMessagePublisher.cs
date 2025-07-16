namespace Inventory.Application.Interfaces
{
    public interface IResilientMessagePublisher
    {
        Task PublishWithResilienceAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default) where T : class;
    }
}
