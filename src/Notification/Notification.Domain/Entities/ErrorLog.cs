namespace Notification.Domain.Entities
{
    public class ErrorLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = string.Empty;
        public string OriginalQueue { get; set; } = string.Empty;
        public string RoutingKey { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public int RetryAttempts { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ErrorTime { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? Resolution { get; set; }
        public bool IsResolved { get; set; }
    }
} 