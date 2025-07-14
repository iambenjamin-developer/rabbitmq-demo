using Inventory.Application.Interfaces;
using Polly;

namespace Inventory.API.Policies
{
    public class ResiliencePolicy : IResiliencePolicy
    {
        private readonly AsyncPolicy _policy;

        public ResiliencePolicy(AsyncPolicy policy)
        {
            _policy = policy;
        }

        public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync(action, cancellationToken);
        }
    }
}
