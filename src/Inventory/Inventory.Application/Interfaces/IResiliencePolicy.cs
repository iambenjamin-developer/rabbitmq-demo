namespace Inventory.Application.Interfaces
{
    public interface IResiliencePolicy
    {
        Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    }
}
