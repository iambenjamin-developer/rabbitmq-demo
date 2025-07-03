namespace Shared.Contracts.Events
{
    public record ProductCreated(long Id, string Name, string Description, decimal Price, int Stock, string Category);
}
