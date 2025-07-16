using Inventory.Domain.Common;

namespace Inventory.Application.Interfaces
{
    public interface IPendingMessageService
    {
        Task<long> SavePendingMessageAsync(string eventType, string routingKey, string payload);
        Task<IEnumerable<PendingMessage>> GetPendingMessagesAsync();
        Task MarkAsProcessedAsync(long messageId);
        Task IncrementRetryCountAsync(long messageId, string? errorMessage = null);
        Task DeleteProcessedMessagesAsync(DateTime olderThan);
    }
}
