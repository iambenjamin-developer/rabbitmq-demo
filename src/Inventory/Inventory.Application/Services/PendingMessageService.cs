using Inventory.Application.Interfaces;
using Inventory.Domain.Common;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Services
{
    public class PendingMessageService : IPendingMessageService
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<PendingMessageService> _logger;

        public PendingMessageService(InventoryDbContext context, ILogger<PendingMessageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<long> SavePendingMessageAsync(string eventType, string routingKey, string payload)
        {
            var pendingMessage = new PendingMessage
            {
                EventType = eventType,
                RoutingKey = routingKey,
                Payload = payload,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0,
                IsProcessed = false
            };

            _context.PendingMessages.Add(pendingMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mensaje pendiente guardado: {EventType} con ID {MessageId}", eventType, pendingMessage.Id);
            return pendingMessage.Id;
        }

        public async Task<IEnumerable<PendingMessage>> GetPendingMessagesAsync()
        {
            return await _context.PendingMessages
                .Where(pm => !pm.IsProcessed)
                .OrderBy(pm => pm.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(long messageId)
        {
            var message = await _context.PendingMessages.FindAsync(messageId);
            if (message != null)
            {
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Mensaje {MessageId} marcado como procesado", messageId);
            }
        }

        public async Task IncrementRetryCountAsync(long messageId, string? errorMessage = null)
        {
            var message = await _context.PendingMessages.FindAsync(messageId);
            if (message != null)
            {
                message.RetryCount++;
                message.ErrorMessage = errorMessage;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Incrementado contador de reintentos para mensaje {MessageId}. Total: {RetryCount}",
                    messageId, message.RetryCount);
            }
        }

        public async Task DeleteProcessedMessagesAsync(DateTime olderThan)
        {
            var messagesToDelete = await _context.PendingMessages
                .Where(pm => pm.IsProcessed && pm.ProcessedAt < olderThan)
                .ToListAsync();

            if (messagesToDelete.Any())
            {
                _context.PendingMessages.RemoveRange(messagesToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Eliminados {Count} mensajes procesados anteriores a {Date}",
                    messagesToDelete.Count, olderThan);
            }
        }
    }
}
