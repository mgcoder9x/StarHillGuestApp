using Bedrock.Application.Ports.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Persistence;

/// <summary>Infrastructure bridge từ module key sang keyed DI registration của Unit of Work.</summary>
internal sealed class ServiceProviderUnitOfWorkResolver(IServiceProvider serviceProvider) : IUnitOfWorkResolver
{
    public IUnitOfWork Resolve(string? persistenceKey)
    {
        if (persistenceKey is null)
        {
            return serviceProvider.GetRequiredService<IUnitOfWork>();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(persistenceKey);
        return serviceProvider.GetRequiredKeyedService<IUnitOfWork>(persistenceKey);
    }
}
